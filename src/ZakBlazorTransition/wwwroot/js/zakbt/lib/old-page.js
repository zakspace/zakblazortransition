/**
 * Keeping the old page on screen, for transitions that show the old and new
 * pages together, like Barba's `sync` mode (see `stack.js`, `slide-over.js`).
 * Blazor swaps the whole page in one go, so `keepOldPage()` puts a still copy of
 * the old one in the layer, where it lasts through the swap, and `enter`
 * animates it over the real new page. The transition sets `solidCover: false`:
 * the copy does the covering.
 *
 * Nothing in the copy runs, loads, plays, or answers to the page's ids, so it's
 * built node by node: with `cloneNode(true)`, a copied custom element would
 * start up again and hydrate a second component, an iframe would load again, a
 * video would restart, and a canvas would be blank.
 *
 * It carries `<body>`'s classes and data attributes, so per-page styles (the
 * demo's `data-ground`) still reach it after the swap. Write those as
 * `[data-…]` or `.class` selectors, not `body…`: the copy is a `<div>`.
 *
 * Limits: it's a still picture (a video stops on its frame), a web component's
 * shadow DOM isn't copied (its children are), and CSS that targets ids or
 * `<html>`'s attributes doesn't reach the copy.
 */
import { panel, pageColor } from './helpers.js';

/** Marks the copy in the layer, so `enter` can find it again. */
const MARK = 'data-old-page';

/** CSS properties a hovered element keeps in the copy: the copy can't be hovered. */
const HOVER = [
  'transform',
  'translate',
  'scale',
  'rotate',
  'opacity',
  'color',
  'background-color',
  'text-decoration-color',
];

/** Computed styles a stand-in takes over, so it sits where the original did. */
const PLACE = [
  'position',
  'top',
  'right',
  'bottom',
  'left',
  'z-index',
  'float',
  'margin-top',
  'margin-right',
  'margin-bottom',
  'margin-left',
  'grid-row-start',
  'grid-row-end',
  'grid-column-start',
  'grid-column-end',
  'align-self',
  'justify-self',
  'vertical-align',
  'border-radius',
  'transform',
];

/** `<body>`'s layout, given to the copy: its blocks must land where they were. */
const LAYOUT = [
  'display',
  'flex-direction',
  'flex-wrap',
  'justify-content',
  'align-items',
  'align-content',
  'row-gap',
  'column-gap',
  'grid-template-columns',
  'grid-template-rows',
  'min-height',
  'padding-top',
  'padding-right',
  'padding-bottom',
  'padding-left',
];

/** Scroll positions that only take once the copy is laid out. */
let restores = [];

/**
 * Puts a still copy of the page in the layer, on a full-screen panel in the
 * page's colour, and returns that panel. Call it in `leave`, before anything
 * moves; animate the panel (or what's in it) as you like.
 */
export function keepOldPage(context) {
  const kept = panel(context.root, { style: { background: pageColor(), overflow: 'hidden' } });
  kept.setAttribute(MARK, '');
  for (const style of document.head.querySelectorAll('style, link[rel="stylesheet"]')) {
    kept.append(style.cloneNode(true));
  }
  kept.append(copyPage(context));
  return kept;
}

/**
 * The panel `keepOldPage()` made, in `enter`. `null` when there's none: the
 * first page with `revealOnLoad` has no old page, so have a fallback.
 */
export const oldPage = (context) => context.root.querySelector(`[${MARK}]`);

/**
 * The page's blocks: the children of `<body>` (header, main, footer…), not the
 * layer, nor `fixed` ones like a skip link or a loader, which stay put on screen.
 */
export function pageBlocks(context) {
  return [...document.body.children].filter((el) => {
    if (!(el instanceof HTMLElement) || el.contains(context.root)) return false;
    return getComputedStyle(el).position !== 'fixed';
  });
}

/**
 * A still copy of the page's blocks as they are now, placed where the page is
 * scrolled to, without the styles `keepOldPage()` keeps. For more copies: of
 * the old page in several pieces (in `leave`), or of the new page (in `enter`,
 * when its styles are in place). Put it in a positioned, page-sized box.
 */
export function copyPage(context) {
  const copy = document.createElement('div');
  for (const { name, value } of document.body.attributes) {
    if (name === 'class' || name.startsWith('data-')) copy.setAttribute(name, value);
  }
  const body = getComputedStyle(document.body);
  for (const property of LAYOUT) copy.style.setProperty(property, body.getPropertyValue(property));
  for (const el of pageBlocks(context)) {
    const still = copyNode(el);
    if (still) copy.append(still);
  }
  copy.inert = true;
  Object.assign(copy.style, { position: 'absolute', inset: `${-window.scrollY}px 0 auto` });
  return copy;
}

/**
 * Puts back the scroll positions inside copied elements. The engine calls it
 * after the page is swapped in and before it paints.
 */
export function flushScrollRestores() {
  const pending = restores;
  restores = [];
  for (const [el, left, top] of pending) el.scrollTo(left, top);
}

/**
 * A still copy of a node and everything in it, or `null` for what isn't seen
 * (scripts, templates).
 */
function copyNode(node) {
  if (!(node instanceof Element)) return node.cloneNode();
  if (node instanceof HTMLScriptElement || node instanceof HTMLTemplateElement) return null;
  if (node instanceof HTMLVideoElement || node instanceof HTMLCanvasElement) return frameOf(node);
  if (node instanceof HTMLElement && node.matches('iframe, audio, embed, object')) {
    return standIn(node, 'div');
  }

  const copy = customElements.get(node.localName) ? plain(node) : node.cloneNode();
  copy.removeAttribute('id');
  copy.removeAttribute('data-entrance');
  if (copy instanceof HTMLImageElement) {
    copy.loading = 'eager';
    copy.decoding = 'sync';
  }
  if (node.matches(':hover') && copy instanceof HTMLElement) {
    const computed = getComputedStyle(node);
    for (const property of HOVER) copy.style.setProperty(property, computed.getPropertyValue(property));
  }
  if (node.scrollLeft || node.scrollTop) restores.push([copy, node.scrollLeft, node.scrollTop]);
  for (const child of node.childNodes) {
    const childCopy = copyNode(child);
    if (childCopy) copy.append(childCopy);
  }
  return copy;
}

/** A div in place of a custom element: its attributes, and the same display. */
function plain(el) {
  const div = document.createElement('div');
  for (const { name, value } of el.attributes) div.setAttribute(name, value);
  div.style.display = getComputedStyle(el).display;
  return div;
}

/** An element of another kind in `el`'s place: its classes, its place, its size. */
function standIn(el, tag) {
  const stand = document.createElement(tag);
  stand.className = el.className;
  const computed = getComputedStyle(el);
  for (const property of PLACE) stand.style.setProperty(property, computed.getPropertyValue(property));
  Object.assign(stand.style, {
    display: computed.display === 'inline' ? 'inline-block' : computed.display,
    width: `${el.offsetWidth}px`,
    height: `${el.offsetHeight}px`,
    flex: 'none',
    boxSizing: 'border-box',
  });
  return stand;
}

/** A canvas in a video's or a canvas's place, painted with what it shows now. */
function frameOf(el) {
  const frame = standIn(el, 'canvas');
  const scale = Math.min(window.devicePixelRatio, 2);
  frame.width = Math.round(el.offsetWidth * scale);
  frame.height = Math.round(el.offsetHeight * scale);
  const [w, h] = el instanceof HTMLVideoElement ? [el.videoWidth, el.videoHeight] : [el.width, el.height];
  if (!w || !h || !frame.width || !frame.height) return frame;

  const fit = getComputedStyle(el).objectFit;
  const fill = Math.max(frame.width / w, frame.height / h);
  const whole = Math.min(frame.width / w, frame.height / h);
  const [dw, dh] =
    fit === 'cover'
      ? [w * fill, h * fill]
      : fit === 'contain'
        ? [w * whole, h * whole]
        : [frame.width, frame.height];
  try {
    frame
      .getContext('2d')
      ?.drawImage(el, (frame.width - dw) / 2, (frame.height - dh) / 2, dw, dh);
  } catch {
    // Not drawable (a lost WebGL context, say): left blank.
  }
  return frame;
}
