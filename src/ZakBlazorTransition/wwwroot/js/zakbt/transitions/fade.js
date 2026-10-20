import { defineTransition } from '../lib/registry.js';
import { panel, sign } from '../lib/helpers.js';

export default defineTransition({
  name: 'fade',
  leave({ root, content }) {
    const cover = panel(root, { style: { background: 'var(--zakbt-fade, #000)' } });
    return gsap.to(cover, { opacity: 0, duration: 0.25, ease: 'power1.out' });
  },
  enter({ root, content }) {
    const cover = document.createElement('div');
    cover.className = 'zakbt-panel';
    cover.style.background = 'var(--zakbt-fade, #000)';
    root.append(cover);
    gsap.set(cover, { opacity: 1 });
    return gsap.to(cover, { opacity: 0, duration: 0.25, ease: 'power1.out', onComplete: () => cover.remove() });
  },
});
