/**
 * ZakBlazorTransition for Blazor: the browser half of the page-transition engine.
 *
 * The .NET side (TransitionHost) drives every step; this module holds the layer,
 * the transition modules and the page-freeze machinery, and answers its calls:
 *
 *   init(element)                  start: layer, listeners, first history entry
 *   consumeIntent()                click and history intent, for the navigation about to run
 *   beginNavigation(request)       cover the page (transition.leave) — awaited before the swap
 *   recordNavigation(traverse)     the new history entry is live: remember where we are
 *   rememberLeave/rememberArrive   what "back" and "forward" will replay
 *   prepare()                      the new page is in the DOM, not painted yet
 *   reveal(...)                    wait for media and the minimum cover time, then reveal
 *   appear()                       show a page that had no transition
 *   saveScroll()                   remember where the visitor was
 *   prefersReducedMotion/has/describe/preload   queries for the .NET side
 *   destroy()                      stop everything
 *
 * Unlike the reference (Astro), clicks are not intercepted here: Blazor's
 * NavigationManager is. This module only stashes where a navigation came from
 * (the pointer, the link's transition) while the click is still fresh, so the
 * handler on the .NET side can pick it up.
 */

import { gsap } from '../vendor/gsap/index.js';
import { SplitText } from '../vendor/gsap/SplitText.js';
import { config } from './config.js';
import { NONE, hasTransition, loadTransition } from './lib/registry.js';
import { mediaReady } from './lib/media.js';
import { keepOldPage, oldPage, flushScrollRestores } from './lib/old-page.js';
import { setupEntrance, runEntrance, teardownEntrance } from './lib/entrance.js';
import { pageColor, farthestCorner, sign } from './lib/helpers.js';

gsap.registerPlugin(SplitText);
// Transitions and page scripts reach GSAP as a global, like the reference site does.
globalThis.gsap = gsap;

/** What each history entry remembers, so "back" and "forward" replay the right transition. */
const KEY = 'zakbt';
const SCROLL_EVENTS = ['wheel', 'touchmove', 'keydown'];
const SCROLL_KEYS = new Set([
  ' ',
  'PageUp',
  'PageDown',
  'Home',
  'End',
  'ArrowUp',
  'ArrowDown',
  'ArrowLeft',
  'ArrowRight',
]);

let root = null;
let state = 'idle'; // idle | leaving | covered | entering
let transition = null;
let requested = config.defaultTransition;
let playing = null;
let covering = null;
let coveredAt = 0;
let run = 0;
let booted = false;
let reducedMotion = false;
let pointer = { x: 0, y: 0, time: Number.NEGATIVE_INFINITY };
let locked = new Set();
let listeners = new AbortController();
let index = 0;
let counter = 0;
let click = null;
let traverse = null;
let context = null;
let entranceEnabled = true;

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const until = (since, ms) => (since + ms > performance.now() ? sleep(since + ms - performance.now()) : Promise.resolve());

/** The page content: the real <main>, never one a transition keeps in the layer. */
const content = () => document.querySelector('main:not([data-zakbt] *)') ?? document.createElement('main');

/** What the engine knows about the page it is on, for the transition to work with. */
function makeContext({ from, to, direction, transition: name, initial }) {
  return {
    root,
    content: content(),
    from,
    to,
    direction,
    transition: name,
    initial: !!initial,
    origin: originOf(),
    reducedMotion,
    entrance: false,
  };
}

/** Where on screen the navigation started: the pointer if it was just used, else the middle. */
function originOf() {
  if (performance.now() - pointer.time < 1000) return { x: pointer.x, y: pointer.y };
  return { x: window.innerWidth / 2, y: window.innerHeight / 2 };
}

/* ------------------------------------------------------------------ history */

const entryState = () => {
  const current = history.state;
  return current && typeof current === 'object' && current[KEY] ? current[KEY] : {};
};

function patchEntry(mutate) {
  const current = history.state;
  const state = current && typeof current === 'object' ? { ...current } : {};
  const entry = { ...(state[KEY] ?? {}) };
  mutate(entry);
  state[KEY] = entry;
  history.replaceState(state, '');
  return entry;
}

/** Gives the current entry an index, so "back" and "forward" can be told apart. */
function ensureIndex() {
  const current = entryState();
  if (typeof current.i === 'number') {
    index = current.i;
    counter = Math.max(counter, index);
    return index;
  }
  index = ++counter;
  patchEntry((entry) => {
    entry.i = index;
  });
  return index;
}

function remember(which, name) {
  if (!name) return;
  patchEntry((entry) => {
    entry[which] = name;
  });
}

function saveScroll() {
  patchEntry((entry) => {
    entry.scroll = Math.round(window.scrollY);
  });
}

/* --------------------------------------------------------------- listeners */

function onPointerDown(event) {
  Object.assign(pointer, { x: event.clientX, y: event.clientY, time: performance.now() });
  preloadFrom(event.target);
}

function onClick(event) {
  if (event.defaultPrevented || event.button !== 0) return;
  if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
  const link = event.target?.closest?.('a[href]');
  if (!(link instanceof HTMLAnchorElement)) return;
  if (link.target && link.target !== '_self') return;
  if (link.hasAttribute('download')) return;
  if (link.closest('[data-astro-reload]')) return;

  const to = new URL(link.href, location.href);
  if (to.origin !== location.origin) return; // another site: the browser handles it
  if (to.href === location.href && !to.hash) {
    event.preventDefault(); // a link to the page you're on: nothing to show
    return;
  }
  // Stash what the navigation will want. Blazor's router intercepts the click itself.
  const name = link.getAttribute('data-transition') ?? link.closest('[data-transition]')?.getAttribute('data-transition');
  click = {
    url: to.href,
    transition: pick(name ?? config.defaultTransition),
    origin: { x: event.clientX, y: event.clientY },
    time: performance.now(),
  };
}

function onPopState(event) {
  const entry = event.state?.[KEY];
  const target = typeof entry?.i === 'number' ? entry.i : null;
  // Blazor walks history back and forth while it settles a traversal: entries
  // without our index are its own steps, not the visitor pressing back or
  // forward. Remembering one would overwrite the real traversal's intent.
  if (target === null) return;
  const direction = target < index ? 'back' : 'forward';
  index = target;
  traverse = {
    direction,
    transition: pick(direction === 'back' ? entry?.leave : entry?.arrive) ?? config.defaultTransition,
    scroll: typeof entry?.scroll === 'number' ? entry.scroll : 0,
  };
}

/** Hovering, focusing or touching a link: load the transition it will play before it's needed. */
function onIntent(event) {
  preloadFrom(event.target);
}

function preloadFrom(target) {
  const name = target?.closest?.('[data-transition]')?.getAttribute('data-transition');
  if (name) loadTransition(pick(name));
}

/** The OS setting can change during a visit: follow it. */
function onMotionChange(event) {
  reducedMotion = event.matches;
  loadTransition(pick(config.defaultTransition));
}

/** Keeps the current history entry's scroll position, so "back" and "forward" can restore it. */
let scrollScheduled = false;
function onScroll() {
  if (scrollScheduled || state !== 'idle') return;
  scrollScheduled = true;
  setTimeout(() => {
    scrollScheduled = false;
    if (state === 'idle' && root) saveScroll();
  }, 150);
}

function holdScroll(event) {
  if (event instanceof KeyboardEvent) {
    if (!SCROLL_KEYS.has(event.key) || event.ctrlKey || event.altKey || event.metaKey) return;
  }
  event.preventDefault();
}

/* ------------------------------------------------------------------ engine */

/**
 * Applies the .NET side's TransitionOptions, so the two halves of the engine
 * agree. Called by the host before `init`; every field is optional.
 */
export function configure(options) {
  if (!options) return;
  if (options.defaultTransition) config.defaultTransition = options.defaultTransition;
  if (typeof options.minCoverTime === 'number') config.minCoverTime = options.minCoverTime;
  if (typeof options.mediaTimeout === 'number') config.mediaTimeout = options.mediaTimeout;
  if (options.reducedMotion) config.reducedMotion = options.reducedMotion;
  if (typeof options.entrance === 'boolean') config.entrance = options.entrance;
  if (options.color) config.color = options.color;
  if (options.revealOnLoad !== undefined) config.revealOnLoad = options.revealOnLoad ?? false;
  if (root && config.color) root.style.setProperty('--zakbt-color', config.color);
}

/** Starts the engine: the layer is the element the host rendered. */
export function init(element) {
  if (root) return;
  root = element;
  root.dataset.ready = '';
  if (config.color) root.style.setProperty('--zakbt-color', config.color);

  const motion = window.matchMedia('(prefers-reduced-motion: reduce)');
  reducedMotion = motion.matches;

  const here = new URL(location.href);
  context = makeContext({ from: here, to: here, direction: 'forward', transition: NONE, initial: true });
  state = root.dataset.state === 'covered' ? 'covered' : 'idle';
  if (state === 'covered') coveredAt = performance.now();

  const signal = listeners.signal;
  motion.addEventListener('change', onMotionChange, { signal });
  window.addEventListener('pointerdown', onPointerDown, { signal, capture: true, passive: true });
  window.addEventListener('click', onClick, { signal, capture: true });
  window.addEventListener('popstate', onPopState, { signal, capture: true });
  window.addEventListener('scroll', onScroll, { signal, passive: true });
  document.addEventListener('pointerover', onIntent, { signal, passive: true });
  document.addEventListener('focusin', onIntent, { signal });

  history.scrollRestoration = 'manual';

  ensureIndex();
  loadTransition(pick(config.defaultTransition));
  if (state === 'covered') {
    lock();
    loadTransition(pick(firstLoad().transition));
  }

  if (document.readyState === 'complete') boot();
  else document.addEventListener('DOMContentLoaded', boot, { signal, once: true });
}

/** Stops the engine: removes its listeners, clears the layer and gives the page back. */
export function destroy() {
  run++;
  listeners.abort();
  listeners = new AbortController();
  stop();
  clear();
  unlock();
  teardownEntrance();
  root = null;
  booted = false;
}

function firstLoad() {
  const reveal = typeof config.revealOnLoad === 'object' ? config.revealOnLoad : {};
  return {
    transition: reveal.transition ?? config.defaultTransition,
    minCoverTime: reveal.minCoverTime ?? 0,
  };
}

/** What actually plays: reduced motion overrides what was asked for. */
function pick(name) {
  if (!reducedMotion) return name;
  return config.reducedMotion === 'fade' && hasTransition('fade') ? 'fade' : NONE;
}

/** Restarts the scroll-in observers, unless the entrance is switched off entirely. */
function updateEntrance() {
  if (config.entrance === false) teardownEntrance();
  else setupEntrance();
}

/** The first page: reveal it if it was rendered covered, else set it up. */
async function boot() {
  if (booted) return;
  booted = true;
  updateEntrance();
  if (state !== 'covered') return appear();

  const loaded = await loadTransition(pick(firstLoad().transition));
  if (!root) return; // destroyed while loading
  transition = loaded ?? null;
  context.transition = transition?.name ?? NONE;
  // The transition's own cover, drawn now, so the wait shows it.
  if (transition?.cover) drawFirstCover(transition);
  await reveal({ waitReady: true, minCoverTime: firstLoad().minCoverTime });
}

/** The first page's cover, from the transition's `cover()`: what its `leave` ends with. */
function drawFirstCover(trans) {
  try {
    trans.cover?.(context);
  } catch (error) {
    console.error(`[zakbt] "${trans.name}" failed in cover().`, error);
  }
  root.dataset.cover = trans.solidCover === false ? 'custom' : 'solid';
}

/**
 * Draws a first page load's cover (`revealOnLoad`) and locks the page, from the
 * transition's `cover()`. The .NET side then waits for media and reveals.
 */
export async function coverOnLoad(name) {
  const loaded = (await loadTransition(name)) ?? (await loadTransition(config.defaultTransition));
  if (!root) return { transition: NONE };
  transition = loaded ?? null;
  context.transition = transition?.name ?? NONE;
  if (transition?.cover) drawFirstCover(transition);
  else root.dataset.cover = transition?.solidCover === false ? 'custom' : 'solid';
  return { transition: context.transition };
}

/**
 * What a navigation asked for: the click's intent if the URL matches, else a
 * history traversal's, else the default. The .NET handler calls this once.
 * `to` is where the navigation is going: a click for that page is used however
 * long the round trip to .NET took; one for a page nobody is navigating to
 * goes stale after a moment, so it can't ambush a later navigation.
 */
export function consumeIntent(to = null) {
  const result = { click: null, traverse: null };
  const matched = click && to && click.url === new URL(to, location.href).href;
  if (matched) {
    // A click for exactly this page is the visitor's intent, however long the
    // round trip through .NET took — and it outranks any traversal left over
    // from Blazor settling a previous one.
    result.click = click;
    click = null;
    traverse = null;
  } else if (traverse) {
    result.traverse = traverse;
    traverse = null;
  } else if (click) {
    if (performance.now() - click.time < 2000) result.click = click;
  }
  click = null;
  return result;
}

/** Whether the visitor prefers reduced motion right now. */
export const prefersReducedMotion = () => reducedMotion;

/** Whether a transition by this name exists, without downloading it. */
export const has = (name) => hasTransition(name) || name === NONE;

/** The settings a transition declares, once it's loaded (for listings and tests). */
export async function describe(name) {
  const transition = await loadTransition(name);
  if (!transition) return null;
  return {
    name: transition.name,
    entrance: typeof transition.entrance === 'number' ? transition.entrance : null,
    solidCover: transition.solidCover !== false,
  };
}

/** Starts downloading a transition ahead of time. */
export function preload(name) {
  loadTransition(name);
}

/**
 * Covers the page with the navigation's transition and resolves when it's
 * fully covered: the .NET side awaits this before letting Blazor swap the page,
 * so the swap happens under the cover.
 */
export async function beginNavigation(request) {
  const runid = ++run;
  requested = request.transition ?? config.defaultTransition;
  const name = pick(requested);
  context = makeContext({
    from: new URL(request.from),
    to: new URL(request.to),
    direction: request.direction === 'back' ? 'back' : 'forward',
    transition: name,
    initial: false,
  });
  if (request.origin && Number.isFinite(request.origin.x)) {
    context.origin = { x: request.origin.x, y: request.origin.y };
  }
  emit('leave');

  if (name === NONE) {
    transition = null;
    return { transition: NONE };
  }

  lock();
  if (!request.traverse) saveScroll();
  const loaded = (await loadTransition(name)) ?? (await loadTransition(config.defaultTransition));
  if (runid !== run) return { transition: name };
  transition = loaded ?? null;
  context.transition = transition?.name ?? NONE;
  if (transition) await leave(transition, context);
  return { transition: context.transition };
}

/** The new history entry is live: give it an index so later "back" navigations can find it. */
export function recordNavigation() {
  ensureIndex();
}

export const rememberLeave = (name) => remember('leave', name);
export const rememberArrive = (name) => remember('arrive', name);

/**
 * The new page is in the DOM but hasn't painted yet: put back copied scroll
 * positions, put the page where the navigation wants it, restart the scroll-in
 * observers, and let the transition set the page's starting state.
 *
 * `scroll` is where a history traversal restores the page to; a fresh
 * navigation starts at the top.
 */
export function prepare({ scroll = null } = {}) {
  flushScrollRestores();
  updateEntrance();
  context.content = content();
  window.scrollTo(0, scroll ?? 0);
  try {
    transition?.prepare?.(context);
  } catch (error) {
    console.error(`[zakbt] "${transition?.name}" failed in prepare().`, error);
  }
}

/**
 * The destination has rendered: wait for what it needs, then reveal it and give
 * the page back. `minCoverTime` is counted from the end of the cover.
 */
export async function reveal({ waitReady = false, minCoverTime = config.minCoverTime } = {}) {
  const runid = run;
  const trans = transition;
  context.content = content();
  updateEntrance();
  if (!trans) return appear();

  await Promise.all([
    waitReady ? waitReady_(trans, config.mediaTimeout) : undefined,
    mediaReady(context.content, { timeout: config.mediaTimeout }),
    until(coveredAt, minCoverTime),
  ]);
  if (runid !== run) return;
  await revealNow(runid, trans);
}

/**
 * Shows a page without a transition: no animation, but the same events and
 * entrance. `request` describes the navigation it belongs to (null on the first page).
 */
export function appear(request = null) {
  stop();
  if (request) {
    context = makeContext({
      from: new URL(request.from),
      to: new URL(request.to),
      direction: request.direction === 'back' ? 'back' : 'forward',
      transition: NONE,
      initial: !!request.initial,
    });
  }
  context.content = content();
  // A first page nothing covered is on screen already: there's nothing to bring in.
  context.entrance = context.initial || config.entrance === false ? false : 0;
  emit('enter');
  runEntrance(context);
  finish();
}

/** Scrolls to a fragment of the current page, for an in-page link. */
export function scrollToHash(fragment) {
  const id = decodeURIComponent(fragment.replace(/^#/, ''));
  const target = document.getElementById(id);
  if (target) target.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

async function revealNow(runid, trans) {
  const ctx = context;
  ctx.content = content();
  ctx.entrance = config.entrance === false ? false : trans.entrance ?? 0;
  state = 'entering';
  label(trans, ctx);
  emit('enter');
  // `enter` builds its visuals synchronously; the layer drops its fill in the same frame.
  const done = play(trans, 'enter', ctx);
  root.dataset.state = 'entering';
  runEntrance(ctx);
  await done;
  if (runid === run) finish();
}

/** Covers the page. Idempotent: navigations that overlap share one cover. */
function leave(trans, ctx) {
  if (state === 'covered') return Promise.resolve();
  if (covering) return covering;
  if (state === 'entering') stop(); // mid-reveal: cover from where it is

  state = 'leaving';
  label(trans, ctx);
  root.dataset.state = 'leaving';
  const solid = trans.solidCover !== false;
  covering = play(trans, 'leave', ctx).then(() => {
    if (state === 'leaving') setCovered(solid);
  });
  return covering;
}

function setCovered(solid) {
  // A solid cover is painted by the layer itself: the transition's panels can go.
  if (solid) root.replaceChildren();
  root.dataset.cover = solid ? 'solid' : 'custom';
  root.dataset.state = 'covered';
  state = 'covered';
  coveredAt = performance.now();
  covering = null;
  playing = null;
  emit('covered');
}

/** Waits until the transition is `ready()` to play, but never longer than `timeout` (ms). */
function waitReady_(trans, timeout) {
  const ready = Promise.resolve(trans.ready?.()).catch((error) => {
    console.error(`[zakbt] "${trans.name}" failed in ready().`, error);
  });
  return Promise.race([ready, sleep(timeout)]);
}

/** Runs `leave` or `enter`. Errors are logged, not thrown: a broken transition mustn't break navigation. */
function play(trans, step, ctx) {
  try {
    const result = trans[step](ctx);
    playing = result instanceof gsap.core.Animation ? result : null;
    return Promise.resolve(result);
  } catch (error) {
    console.error(`[zakbt] "${trans.name}" failed in ${step}().`, error);
    return Promise.resolve();
  }
}

/** Stops what's animating on the layer and the content, and clears the layer. */
function stop() {
  playing?.kill();
  playing = null;
  gsap.killTweensOf([...root.querySelectorAll('*'), context?.content]);
  root.replaceChildren();
}

/** Back to normal: clear the layer, give input, scrolling and focus back. */
function finish() {
  clear();
  unlock();
  if (!context.initial) {
    const main = context.content;
    if (main && !main.hasAttribute('tabindex')) main.tabIndex = -1;
    main?.focus?.({ preventScroll: true });
  }
  emit('idle');
}

function clear() {
  if (!root) return;
  root.replaceChildren();
  root.dataset.state = 'idle';
  delete root.dataset.cover;
  delete root.dataset.transition;
  delete root.dataset.direction;
  state = 'idle';
  covering = null;
  playing = null;
  transition = null;
}

/**
 * Freezes the page while a transition runs: it can't be scrolled, and nothing
 * but the layer is reachable by pointer, keyboard or screen reader.
 */
function lock() {
  for (const type of SCROLL_EVENTS) window.addEventListener(type, holdScroll, { passive: false });
  for (const el of document.body.children) {
    if (el === root || el.contains(root) || el.hasAttribute('inert')) continue;
    el.setAttribute('inert', '');
    locked.add(el);
  }
}

function unlock() {
  for (const type of SCROLL_EVENTS) window.removeEventListener(type, holdScroll);
  for (const el of locked) el.removeAttribute('inert');
  locked.clear();
}

/** Exposes the running transition on the layer, for CSS: `.zakbt[data-transition='columns']`. */
function label(trans, ctx) {
  root.dataset.transition = trans.name;
  root.dataset.direction = ctx.direction;
}

function emit(phase) {
  document.dispatchEvent(new CustomEvent(`zakbt:${phase}`, { detail: context }));
}

export { gsap, pageColor, farthestCorner, sign, keepOldPage, oldPage };
