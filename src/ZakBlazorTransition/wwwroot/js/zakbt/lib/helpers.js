/**
 * Small helpers for writing transitions.
 * (`gsap` is the global the vendored GSAP build sets; it is loaded by zakbt.js.)
 */

/** The persistent layer above the page. */
export const layer = () => document.querySelector('[data-zakbt]');

/** The page content: the real <main>, never one a transition keeps in the layer. */
export const content = () =>
  document.querySelector('main:not([data-zakbt] *)') ?? document.createElement('main');

/**
 * Adds a full-screen panel (a filled element) to the transition layer and returns
 * it. The engine clears the layer once a transition is over.
 *
 * @param {ParentNode} root
 * @param {{ className?: string, style?: CSSStyleDeclaration | Record<string,string> }} [options]
 */
export function panel(root, { className, style } = {}) {
  const el = document.createElement('div');
  el.className = className ? `zakbt-panel ${className}` : 'zakbt-panel';
  if (style) Object.assign(el.style, style);
  root.append(el);
  return el;
}

/** Distance from a point to the farthest corner of the viewport: a circle this big covers it. */
export function farthestCorner(x, y) {
  const w = window.innerWidth;
  const h = window.innerHeight;
  return Math.hypot(Math.max(x, w - x), Math.max(y, h - y));
}

/** +1 going forward, -1 going back: multiply a movement by it to mirror it on "back". */
export const sign = (context) => (context.direction === 'back' ? -1 : 1);

/** The visitor asked for less motion (OS setting). */
export const prefersReducedMotion = () =>
  window.matchMedia('(prefers-reduced-motion: reduce)').matches;

/** The page's background colour: `<body>`'s, or `<html>`'s if the body has none. */
export function pageColor() {
  const transparent = (color) => !color || color === 'transparent' || color === 'rgba(0, 0, 0, 0)';
  for (const el of [document.body, document.documentElement]) {
    const color = getComputedStyle(el).backgroundColor;
    if (!transparent(color)) return color;
  }
  return '#fff';
}

/** Reads a CSS custom property, trimmed, with a fallback. */
export function cssColor(property, fallback = '#000', element = document.documentElement) {
  return getComputedStyle(element).getPropertyValue(property).trim() || fallback;
}

/** Resolves once `ms` have passed. */
export const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
