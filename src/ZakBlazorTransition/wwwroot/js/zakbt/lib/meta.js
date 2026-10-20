/**
 * @param {string} [transition] - transition name
 * @param {boolean} [reduced] - prefers-reduced-motion
 */
export function metaFor(transition, reduced) {
  const desc = [];
  if (reduced) desc.push('Reduced motion');
  if (transition) desc.push(transition);
  return { transition, reduced, description: desc.join(' — ') };
}
