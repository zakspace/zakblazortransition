# ZakBlazorTransition

A page-transition engine for Blazor: twenty-one transitions that play **during real
Blazor route navigation** — the router swaps pages under a cover, and the same
animation reveals the destination. Press the browser's back button and what you
watched replays, in reverse.

Live demo project: `src/ZakBlazorTransition.Demo` (server prerender + Interactive WebAssembly).

Installing the library in your own app? See the packaged consumer guide at
[`src/ZakBlazorTransition/README.md`](src/ZakBlazorTransition/README.md); this file is the full solution/dev overview.

```
┌─────────────────────┐   ┌──────────────────────────┐   ┌────────────────────────┐
│ ZakBlazorTransition.Core │ ← │ ZakBlazorTransition (library) │ ← │ ZakBlazorTransition.Demo    │
│ options, catalog,    │   │ wwwroot/js engine + GSAP  │   │ editorial pages: index  │
│ service, contracts   │   │ + three.js, host & link   │   │ of 21, /transition/{n}  │
│ (no web deps)        │   │ components, runtime glue  │   │ detail routes, grounds  │
└─────────────────────┘   └──────────────────────────┘   └────────────────────────┘
```

## Quick start

Install the NuGet package (this also brings in `ZakBlazorTransition.Core`, and ships the
JS engine and the already-vendored GSAP/three.js as static web assets — no Node required):

```powershell
dotnet add package ZakBlazorTransition
```

```csharp
// Program.cs — call on BOTH sides when you use prerendering, so the first paint
// matches what the client engine expects.
builder.Services.AddZakBT(options =>
{
    options.DefaultTransition = TransitionNames.Peel;
    options.ReducedMotion = ReducedMotionMode.Fade;
    options.RevealOnLoad = new RevealOnLoadOptions { Transition = TransitionNames.Curtain };
});
```

```razor
@* Routes.razor — the layer above the router, and it drives every navigation. *@
<TransitionHost />

<Router AppAssembly="typeof(Program).Assembly"> … </Router>
```

```razor
@* Any link names its transition; hover and focus start downloading it early. *@
<a href="/about" data-transition="wipe">About</a>

@* Or let the library do the marking: *@
<TransitionLink Href="/about" Transition="circle">About</TransitionLink>
<TransitionIndex /> @* the grouped index of all twenty-one *@
```

Also reference the engine's stylesheet in `App.razor`:

```html
<link rel="stylesheet" href="/_content/ZakBlazorTransition/css/zakbt.css" />
```

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

`AddZakBT` validates the options and throws on unknown transition names or
non-positive timeouts.

`ITransitionService` exposes `Phase`, `IsTransitioning` and `PhaseChanged` to your
components; pages can call `NotifyPageRendered()` to hint that their content is on
screen (the engine also has a JS-side render wait with `RenderTimeoutMs` as backstop).

## The twenty-one transitions

**Overlay** — a panel covers the screen: `curtain`, `wipe`, `circle`, `columns`, `typewriter`
**Page composition** — the pages themselves move: `stack`, `slide-over`, `peel`, `slices`, `frame`, `carousel`, `cube`, `corner`, `tear`
**WebGL** — three.js paints the swap on a canvas: `dither`, `dissolve`, `ink`, `spiral`, `velvet`, `particles`, `channel`

Plus the internal `fade` (reduced motion) and `none` (instant swap).

Quality behaviour: three.js is loaded *only* when a WebGL transition is first
requested; the renderer takes WebGPU → WebGL2 → a GSAP cross-fade automatically.
During a transition the page is `inert`, scrolling is held, and the layer is
`aria-hidden`; afterwards focus lands on `<main>`. History entries remember which
transition each page arrived with and left by, so back/forward replay the effect
that actually played, mirrored.

## Adding a transition

1. Drop a module in `src/ZakBlazorTransition/wwwroot/js/zakbt/transitions/{name}.js`:

   ```js
   import { defineTransition } from '../lib/registry.js';
   import { panel } from '../lib/helpers.js';

   export default defineTransition({
     name: 'my-transition',
     solidCover: true,      // false when you paint your own cover/canvas
     entrance: null,        // seconds into enter() before page content may come in
     leave(context) { /* cover the page; return a GSAP timeline */ },
     enter(context) { /* reveal the new page */ },
   });
   ```

   The registry loads `/…/js/transitions/{name}.js` lazily by that filename.

2. Teach the .NET side: add a `TransitionNames` constant and a
   `TransitionDescriptor` row to `TransitionCatalog` (group, entrance, WebGL flag).

3. Link it: `<a href="…" data-transition="my-transition">`. The engine preloads on
   hover/focus, resolves reduced motion, and records it in history — nothing else.

Useful helpers: `keepOldPage()` / `copyPage()` (still copies of the live page),
`pageColor()`, `farthestCorner()`, `sign()`, and `shaderCover()` for GL transitions.

## Demo website

- `/` — the index of all twenty-one; every row plays itself.
- `/transition/{name}` — a detail page per transition (unknown names render the
  styled 404). Its *Back* button goes through history, so the arrival replays in
  reverse. Prev/next links tour the catalog.
- Page grounds (`white` / `grey` / `night`) ride on `body[data-ground]`, which the
  old-page copies carry into the layer — departing pages keep their colour mid-transition.

## Building and testing

```powershell
dotnet build ZakBlazorTransition.slnx                    # library, demo, tests
dotnet test tests/ZakBlazorTransition.Core.Tests         # options, catalog, service, JSON contract
dotnet test tests/ZakBlazorTransition.PlaywrightTests    # end-to-end in headless Chromium
```

Playwright tests start the demo on port `5310` themselves (build first; they run
`dotnet run --no-build`). One-time browser install:

```powershell
& tests\ZakBlazorTransition.PlaywrightTests\bin\Debug\net10.0\.playwright\node\win32_x64\node.exe `
  tests\ZakBlazorTransition.PlaywrightTests\bin\Debug\net10.0\.playwright\package\cli.js install chromium
```

Run the demo: `dotnet run --project src/ZakBlazorTransition.Demo` → http://localhost:5262.

The `gsap` and `three.js` ESM copies under `src/ZakBlazorTransition/wwwroot/js/vendor` are
committed and shipped as static web assets — the library builds, packs and runs without
Node or npm at any point. GSAP and three.js are bundled under their own licenses.
