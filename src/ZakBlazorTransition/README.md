![ZakBlazorTransition](https://zakicons.vercel.app/logo.svg)

# ZakBlazorTransition

Page transitions for Blazor that play **during real route navigation** — the router
swaps the page under a cover, and the same animation reveals the destination. Press
the browser's back button and what you watched replays, in reverse.

Twenty-one effects ship with the engine (CSS/GSAP and optional WebGL), driven by a
single `<TransitionHost />` above your router. Your pages never coordinate with it.

## Install

```powershell
dotnet add package ZakBlazorTransition
```

This also brings in `ZakBlazorTransition.Core` (options, catalog, contracts). The
package ships the Razor components, the JS engine, and the already-vendored GSAP and
three.js as static web assets — **no Node required**.

## Quick start

Register the service in `Program.cs`. With prerendering, call it on **both** the
server and the `*.Client` side with the same options — the first paint is planned
server-side.

```csharp
builder.Services.AddZakBT(options =>
{
    options.DefaultTransition = TransitionNames.Peel;
    options.ReducedMotion = ReducedMotionMode.Fade;
    options.RevealOnLoad = new RevealOnLoadOptions { Transition = TransitionNames.Curtain };
});
```

Mount one host above the router in `Routes.razor`:

```razor
<TransitionHost />

<Router AppAssembly="typeof(Program).Assembly"> … </Router>
```

Reference the engine's stylesheet in `App.razor` (in the `<head>`, before your own CSS):

```html
<link rel="stylesheet" href="/_content/ZakBlazorTransition/css/zakbt.css" />
```

Name a transition on any link — that is the entire page-level API:

```razor
<a href="/about" data-transition="circle">About</a>

@* or let the library components mark the link: *@
<TransitionLink Href="/about" Transition="circle">About</TransitionLink>

@* or render the grouped index of all twenty-one (links to /transition/{name} by default): *@
<TransitionIndex />
```

A link without `data-transition` plays `DefaultTransition`; `none` swaps instantly.

## Configuration — `TransitionOptions`

| Setting | Default | What it does |
|---|---|---|
| `DefaultTransition` | `peel` | Plays when a link names none. `none` swaps instantly. |
| `MinCoverTimeMs` | `0` | Keeps the page covered at least this long (ms) after the cover ends. |
| `RevealOnLoad` | off | Covers the very first load and reveals it — a preloader driven by the same pipeline. |
| `ReducedMotion` | `None` | What visitors preferring reduced motion get: `None` (instant swap) or `Fade` (cross-fade). |
| `MediaTimeoutMs` | `1500` | Cap on waiting for above-the-fold images/fonts before revealing. |
| `RenderTimeoutMs` | `4000` | Cap on waiting for the destination to render, so a lost signal can't keep the page covered. |
| `EntranceEnabled` | `true` | Run `data-entrance` content animations as pages are revealed. |
| `CoverColor` | `#111111` | The colour painted before a transition draws its own cover. |

`AddZakBT` validates options and throws at startup on unknown transition names or
non-positive timeouts — not in the browser.

## The twenty-one transitions

- **Overlay** (a panel covers the screen): `curtain`, `wipe`, `circle`, `columns`, `typewriter`
- **Page composition** (the pages themselves move): `stack`, `slide-over`, `peel`, `slices`, `frame`, `carousel`, `cube`, `corner`, `tear`
- **WebGL** (three.js paints the swap on a canvas): `dither`, `dissolve`, `ink`, `spiral`, `velvet`, `particles`, `channel`

Plus the internal `fade` (reduced motion) and `none` (instant swap).

## Adding your own transition

There is no drop-in registration yet: a new effect is added to the library itself
(fork or vendor its source), in two places. First the module, whose file name must
match its `name`:

```js
// src/ZakBlazorTransition/wwwroot/js/zakbt/transitions/my-effect.js
import { defineTransition } from '../lib/registry.js';

export default defineTransition({
  name: 'my-effect',
  leave(context) { /* cover the screen; return a GSAP timeline */ },
  enter(context) { /* reveal the new page */ },
});
```

Then register it in the library's `wwwroot/js/zakbt/lib/registry.js` `loaders` map
(`'my-effect': () => import('../transitions/my-effect.js')`), and add a
`TransitionNames` constant and a `TransitionCatalog.All` row on the .NET side. Link it
with `data-transition="my-effect"` and the engine preloads, resolves reduced motion,
and records it in history like any built-in.

## Accessibility and performance

three.js is loaded only when a WebGL transition is first requested; the renderer takes
WebGPU → WebGL2 → a GSAP cross-fade automatically. During a transition the page is
`inert`, scrolling is held, and the layer is `aria-hidden`; afterwards focus lands on
`<main>`. History entries remember which transition each page arrived with and left by,
so back/forward replay the effect that actually played, mirrored.

## License

MIT. GSAP and three.js are bundled under their own licenses.
