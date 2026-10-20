/* Demo-only helpers. The engine never looks at these; the demo pages call them. */
(() => {
  const demo = {
    /** Sets the editorial ground (page background) of the current page. */
    setGround(ground) {
      if (!ground) delete document.body.dataset.ground;
      else document.body.dataset.ground = ground;
    },

    /**
     * Scrolls an in-page section into view without changing the URL. The docs TOC
     * uses this instead of `#fragment` links so a same-page jump never trips the
     * engine's post-navigation scroll-to-top.
     */
    scrollToId(id) {
      if (!id) return;
      const el = document.getElementById(id);
      if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    },

    /**
     * Leaves the current page the way a visitor would: through history, so the
     * engine replays the transition that got us here — in reverse. With no page
     * behind us in the session, fall back to a plain trip to the index.
     */
    goBack(fallback) {
      if (history.length > 1) history.back();
      else location.href = fallback ?? '/';
    },

    /**
     * Copies text to the clipboard. Prefers the async Clipboard API (available on
     * the secure localhost origin); falls back to a hidden textarea + execCommand.
     * Returns true on success so the caller can show feedback.
     */
    async copyText(text) {
      try {
        if (navigator.clipboard && window.isSecureContext) {
          await navigator.clipboard.writeText(text);
          return true;
        }
      } catch (e) {
        /* fall through to the legacy path */
      }
      try {
        const ta = document.createElement('textarea');
        ta.value = text;
        ta.setAttribute('readonly', '');
        ta.style.position = 'fixed';
        ta.style.top = '-1000px';
        ta.style.opacity = '0';
        document.body.appendChild(ta);
        ta.select();
        const ok = document.execCommand('copy');
        document.body.removeChild(ta);
        return ok;
      } catch (e) {
        return false;
      }
    },
  };

  window.zakbtDemo = window.zakbtDemo ?? demo;
})();
