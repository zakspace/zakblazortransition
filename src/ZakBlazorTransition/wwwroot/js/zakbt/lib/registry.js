/**
 * The transition registry. Every module in `transitions/` is listed here by its
 * file name, which must also be its `name` (mirrored by ZakBlazorTransition.Core's
 * `TransitionCatalog`).
 *
 * Transitions are loaded on demand: when a link that uses one is hovered,
 * focused or touched, or at the latest when the navigation starts. A heavy
 * transition (one that pulls in Three.js) then only costs the visitors who
 * trigger it.
 */

/** Reserved name: swap the page with no transition (`data-transition="none"`). */
export const NONE = 'none';

const loaders = {
  // Overlay
  curtain: () => import('../transitions/curtain.js'),
  wipe: () => import('../transitions/wipe.js'),
  circle: () => import('../transitions/circle.js'),
  columns: () => import('../transitions/columns.js'),
  typewriter: () => import('../transitions/typewriter.js'),
  // Page composition
  stack: () => import('../transitions/stack.js'),
  'slide-over': () => import('../transitions/slide-over.js'),
  peel: () => import('../transitions/peel.js'),
  slices: () => import('../transitions/slices.js'),
  frame: () => import('../transitions/frame.js'),
  carousel: () => import('../transitions/carousel.js'),
  cube: () => import('../transitions/cube.js'),
  corner: () => import('../transitions/corner.js'),
  tear: () => import('../transitions/tear.js'),
  // WebGL
  dither: () => import('../transitions/dither.js'),
  dissolve: () => import('../transitions/dissolve.js'),
  ink: () => import('../transitions/ink.js'),
  spiral: () => import('../transitions/spiral.js'),
  velvet: () => import('../transitions/velvet.js'),
  particles: () => import('../transitions/particles.js'),
  channel: () => import('../transitions/channel.js'),
  // Reduced motion
  fade: () => import('../transitions/fade.js'),
};

/** Transitions loading or loaded, so each is downloaded once. */
const loaded = new Map();
const warned = new Set();

/** Warns about a mistake once per visit, not on every navigation. */
function warnOnce(message) {
  if (warned.has(message)) return;
  warned.add(message);
  console.warn(`[zakbt] ${message}`);
}

/** Whether `transitions/<name>.js` exists, without loading it. */
export const hasTransition = (name) => Object.hasOwn(loaders, name);

/** Loads a transition by name. Resolves to `undefined` if there's none (or it fails to load). */
export function loadTransition(name) {
  const pending = loaded.get(name);
  if (pending) return pending;

  const load = loaders[name];
  if (!load) {
    if (name !== NONE) warnOnce(`No transition named "${name}".`);
    return Promise.resolve(undefined);
  }

  const promise = load()
    .then(({ default: transition }) => {
      if (!transition?.leave || !transition?.enter) {
        warnOnce(`transitions/${name}.js has no default export made with defineTransition().`);
        return undefined;
      }
      if (transition.name !== name) {
        warnOnce(`transitions/${name}.js is named "${transition.name}": use the file name.`);
      }
      return transition;
    })
    .catch((error) => {
      console.error(`[zakbt] Couldn't load the "${name}" transition.`, error);
      loaded.delete(name);
      return undefined;
    });

  loaded.set(name, promise);
  return promise;
}

/** Wraps a transition's definition: gives transition files a single shape to conform to. */
export const defineTransition = (transition) => transition;
