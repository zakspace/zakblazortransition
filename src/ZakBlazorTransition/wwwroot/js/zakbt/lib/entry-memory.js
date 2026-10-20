/**
 * History bookkeeping: which entry we are on, which transition each entry used,
 * and where the visitor had scrolled.
 */

const KEY = 'zakbt';

let counter = 0;

/** The entry's own state, never clobbering what the router stored there. */
export function patch(mutate) {
  const current = history.state;
  const state = current && typeof current === 'object' ? { ...current } : {};
  const entry = { ...(state[KEY] ?? {}) };
  mutate(entry);
  state[KEY] = entry;
  history.replaceState(state, '');
  return entry;
}

/** The current entry's zakbt bookkeeping. */
export function current() {
  const state = history.state;
  return state && typeof state === 'object' && state[KEY] ? state[KEY] : {};
}

/**
 * Gives the current entry an index and starts a new one for fresh navigations,
 * so "back" and "forward" can be told apart without guessing.
 */
export function ensureIndex() {
  const entry = current();
  if (typeof entry.i === 'number') return entry.i;
  const index = ++counter;
  patch((e) => {
    e.i = index;
  });
  return index;
}

/** Marks the current entry with the index we just decided on (traverse: reuse it). */
export function setIndex(index) {
  patch((e) => {
    e.i = index;
    counter = Math.max(counter, index);
  });
}

/** Remembers the transition that is leaving this entry / arriving on it. */
export function remember(which, name) {
  if (!name) return;
  patch((e) => {
    e[which] = name;
  });
}

/** Where the visitor was on this entry, saved while they scroll. */
export function saveScroll(y) {
  patch((e) => {
    e.scroll = Math.round(y);
  });
}

/** Where the visitor was on this entry, read right after a popstate. */
export const scrollOf = (entry) => (typeof entry?.scroll === 'number' ? entry.scroll : 0);

/** Reads a fresh entry's bookkeeping after a popstate, before anything else writes it. */
export function read(state) {
  return state && typeof state === 'object' && state[KEY] ? state[KEY] : {};
}
