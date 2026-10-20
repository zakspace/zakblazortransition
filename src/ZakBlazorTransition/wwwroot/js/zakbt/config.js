/**
 * Engine defaults. The .NET side owns the real settings (TransitionOptions) and
 * pushes them here with `configure()` before `init()`; these are the fallbacks
 * the module uses when it runs on its own (a test page, a plain site).
 */
const zakbtConfig = {
  defaultTransition: 'peel',
  minCoverTime: 0,
  revealOnLoad: false,
  reducedMotion: 'none',
  mediaTimeout: 1500,
  entrance: true,
  color: null,
};

export const config = zakbtConfig;
