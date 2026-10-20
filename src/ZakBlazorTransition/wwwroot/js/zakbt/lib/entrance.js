/**
 * The entrance: how each page's content comes in, on every page.
 *
 * Mark elements in a page's markup, and they come in as the page is revealed
 * (the ones on screen) or as they scroll into view (the ones further down):
 *
 *   <p data-entrance>…</p>                   fades in, moving a little (rising, by default)
 *   <h1 data-entrance="lines">…</h1>         each line comes in from behind a mask
 *   <div data-entrance="image"><img></div>   the frame opens (upwards), the image zooms out
 *
 * When it starts is up to each transition (its `entrance`); how it moves is set
 * here, including which way after each transition (`BY_TRANSITION`).
 */

/** Where something comes in from. */
const EASE = 'expo.out';
const STAGGER = 0.03;
const MOVE = { distance: 40, duration: 1.2 };
const LINES = { duration: 1.2, stagger: 0.1, travel: 150 };
const MASK_BLEED = 0.25;
const IMAGE = { duration: 1.4, zoom: 1.25, zoomDuration: 1.8 };
const FADE = { duration: 1, ease: 'power1.out' };
const SCROLL_OFFSET = 10;
const DEFAULT = { from: 'below' };

/** After these transitions, the content comes in its own way, to match them. */
const BY_TRANSITION = {
  curtain: { from: 'below', back: 'above' },
  stack: { from: 'here' },
};

/** An image frame clipped away entirely, ready to open from each side. */
const CLOSED = {
  below: 'inset(100% 0% 0% 0%)',
  above: 'inset(0% 0% 100% 0%)',
  left: 'inset(0% 100% 0% 0%)',
  right: 'inset(0% 0% 0% 100%)',
};

let observer = null;

const reduced = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches;

/** The page's marked elements, in page order. */
const targets = (scope = document) => [...scope.querySelectorAll('[data-entrance]')];

/** On screen, or above it: the elements the reveal brings in (the rest wait for the scroll). */
const onScreen = (el) => el.getBoundingClientRect().top < window.innerHeight;

/** The style after this navigation's transition: going back, its `back` if it has one. */
function styleAfter({ transition, direction }) {
  const { from, back } = BY_TRANSITION[transition] ?? DEFAULT;
  if (direction !== 'back') return { from };
  return { from: back ?? from };
}

/** GSAP's starting offset for coming in from `side`: `amount` px, or % of the element's own size. */
function away(side, amount, unit) {
  const axis = side === 'below' || side === 'above' ? 'y' : 'x';
  const sign = side === 'below' || side === 'right' ? 1 : -1;
  return { [unit === '%' ? `${axis}Percent` : axis]: amount * sign };
}

/** An image frame's clip-path, closed (clipped away towards `from`) or open. */
const clip = (from, open) => (open ? 'inset(0% 0% 0% 0%)' : CLOSED[from]);

/** Hides an element further down the page until it scrolls into view. */
function hide(el) {
  if (el.dataset.entrance === 'image') gsap.set(el, { clipPath: clip('below', false) });
  else gsap.set(el, { opacity: 0 });
}

/** Brings an element in, `delay` seconds from now, the `style` way. */
function show(el, delay, { from }) {
  const kind = el.dataset.entrance;

  if (from === 'here') {
    gsap.fromTo(el, { opacity: 0 }, { opacity: 1, ...FADE, delay, clearProps: 'opacity' });
  } else if (kind === 'lines') {
    gsap.set(el, { opacity: 1 });
    const split = SplitText.create(el, { type: 'lines', mask: 'lines' });
    gsap.set(split.masks, {
      overflow: 'visible',
      clipPath: `inset(-${MASK_BLEED}em -0.1em)`,
    });
    gsap.from(split.lines, {
      ...away(from, LINES.travel, '%'),
      duration: LINES.duration,
      stagger: LINES.stagger,
      ease: EASE,
      delay,
      onComplete: () => split.revert(),
    });
  } else if (kind === 'image') {
    gsap.fromTo(
      el,
      { clipPath: clip(from, false) },
      {
        clipPath: clip(from, true),
        duration: IMAGE.duration,
        ease: EASE,
        delay,
        clearProps: 'clipPath',
      },
    );
    gsap.fromTo(
      el.querySelector('img'),
      { scale: IMAGE.zoom },
      { scale: 1, duration: IMAGE.zoomDuration, ease: EASE, delay, clearProps: 'transform' },
    );
  } else {
    gsap.fromTo(
      el,
      { opacity: 0, ...away(from, MOVE.distance, 'px') },
      {
        opacity: 1,
        x: 0,
        y: 0,
        duration: MOVE.duration,
        ease: EASE,
        delay,
        clearProps: 'opacity,transform',
      },
    );
  }
}

/** The page is in the DOM, not yet revealed: hide what's further down, bring it in on scroll. */
export function setupEntrance(scope = document) {
  teardownEntrance();
  if (reduced()) return;

  const later = targets(scope).filter((el) => !onScreen(el));
  if (!later.length) return;
  later.forEach(hide);

  observer = new IntersectionObserver(
    (entries) => {
      for (const entry of entries) {
        if (!entry.isIntersecting) continue;
        observer.unobserve(entry.target);
        show(entry.target, 0, DEFAULT);
      }
    },
    { rootMargin: `0px 0px -${SCROLL_OFFSET}% 0px` },
  );
  later.forEach((el) => observer.observe(el));
}

/** The page is leaving: stop watching for scroll-ins. */
export function teardownEntrance() {
  observer?.disconnect();
  observer = null;
}

/** The page is being revealed: bring in what's on screen. */
export function runEntrance(context) {
  const { entrance, reducedMotion } = context;
  if (entrance === false || reducedMotion) return;
  const style = styleAfter(context);
  const elements = targets(context.content ?? document).filter(onScreen);
  elements.forEach((el, i) => {
    const place = style.from === 'above' ? elements.length - 1 - i : i;
    show(el, entrance + place * STAGGER, style);
  });
}
