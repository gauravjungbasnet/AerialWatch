# Frontend folder guide

Start at `src/main.tsx`, then `src/app/App.tsx`. The app shell selects a module from `src/app/registry.ts`.

| Folder | Owns |
|---|---|
| `src/shared/controls/` | Reusable Windmill controls: buttons, forms, cards, tables, feedback and dialogs |
| `src/shared/components/navigation/` | Generic sidebar renderer and navigation types |
| `src/shared/api/` | HTTP transport, API error handling and common error contract |
| `src/shared/lib/`, `src/shared/styles/` | Helpers, theme and design tokens |
| `src/app/layout/` | Platform layout, header and sidebar composition |
| `src/app/components/` | App launcher and selected-app identity |
| `src/app/registry.ts` | Launcher entries and the currently implemented app |
| `src/modules/watch-feral-pigs/` | WFP module definition, routes and navigation |
| `src/modules/watch-feral-pigs/features/` | Dashboard, planner, surveys, admin and about views, hooks and helpers |
| `src/modules/watch-feral-pigs/components/` | WFP components used by more than one feature (status badge, survey table, research blocks) |
| `src/modules/watch-feral-pigs/domain/` | WFP rules and labels with no React or API: cameras, seasons, time windows, box appearance |
| `src/modules/watch-feral-pigs/api/` | `contracts/` (DTO mirrors per feature), `endpoints/` (calls per feature), `keys.ts` (every query key), `queries/` (server-state hooks used by more than one feature); `types.ts` and `client.ts` re-export them |

Shared code never imports an app module. Modules use shared controls and their own API hooks. The platform shell reads a module definition rather than owning feature routes or sidebar links. Browser URLs are module-scoped: / redirects to /wfp; WFP features are under /wfp.

WWL (/wwl) and WGN (/wgn) have their own coming-soon modules, navigation and reserved API clients. The current app is resolved from the URL, including direct links, refresh and browser history. Implement future feature code inside the corresponding module.

Run `npm run dev`, `npm run build`, `npm run lint`, `npm test` and `npm run format` from this folder. See [architecture](../../docs/06-architecture.md) for full dependency rules and [source README](../README.md) for backend setup.

## Module endpoints and access

Each app owns its api/client.ts. WFP exports wfpApi with /api/wfp; WWL and WGN reserve /api/wwl and /api/wgn. Shared transport has no feature endpoints. WFP query keys begin with wfp.

Backend routes follow Controllers/<Module>/<Component>Controller.<Method> -> /api/{module}/{component}/{method}, lower camel case. For example PlannerController.GetWindows becomes GET /api/wfp/planner/getWindows; SurveysController.Create becomes POST /api/wfp/surveys/create. See docs/06-architecture.md for the route table.

Module controllers inherit AppControllerBase. Development has a local Admin identity only when Authentication:DevelopmentBypass is enabled. Outside Development authentication is required, anonymous calls return 401, and admin endpoints require Admin. No production sign-in is implemented yet. Restart the API after pulling these controller changes; old unprefixed API routes are replaced.

Results workspace (29 Oct 2026): module routes can set handle.fullBleed to request unpadded full-height shell content. Results uses a map background only for wide/tall viewports; short desktops and phones keep panels in normal flow. Shared Modal uses the native dialog top layer for focus containment and background inertness, with bounded content scrolling.

Photo bottom sheets are non-modal shared controls: keep the map usable, focus the sheet title on entry, support Close/Escape and restore trigger focus. Results marker selection filters the grid even for zero-confirmed photos; Show all returns to photos with pigs.

Review drawing: hold Ctrl (Windows/Linux) or Cmd (Mac) while dragging to temporarily draw; N and the Draw tool still work. Ctrl/Cmd+Z undoes new ranger drawings on the current photo; Ctrl/Cmd+Y or Ctrl/Cmd+Shift+Z redo. History lasts for the open review session; AI decision reset remains U. Run node --test tests/review-drawing.test.mjs for drawing-history tests.


## Shared foundations (29 Oct 2026)

- `shared/types/training.ts` owns neutral training contracts and labels; API contracts import this directly. The former UI paths retain compatibility exports.
- `shared/types/routes.ts` owns `AppRoute` and `RouteLayout`; module routes declare `handle.fullBleed` through this contract, and the shell consumes it.
- `shared/imaging/annotations/geometry.ts` is used by review drawing and paired resizing. Geometry remains in original-image pixels; WFP owns when and how to persist paired changes.
- `modules/watch-feral-pigs/domain/cameras.ts` owns cross-feature camera labels/options. Feature-specific rules stay local.

This is the next foundation slice of docs/17, not completion of the viewer/map extraction. Original changed files are saved under `evidence/2026-10-29-frontend-foundations/before/` because this checkout has no Git metadata.

Shared read-only image display: `shared/imaging/viewer/AnnotatedImage.tsx` accepts src, accessible alt text, caption, optional original-pixel width/height, and neutral ImageAnnotation records (Rect plus key/label/color/dashed). No API/domain imports, zoom controls or wheel zoom. Results maps its own detections/twin records into these props. Source changes reset load/failure state. The editable WFP Review/ImageViewer remains separate pending the wider imaging extraction. Photo findings use shared Modal, opened from the count bar; its Escape handling takes precedence over the non-modal BottomSheet.

## Configurable shared imaging (30 Oct 2026)

Review and Results now use the same rendering/interaction engine and comparison component. This supersedes the earlier note that the editable viewer is still separate.

- `shared/imaging/viewer/ImageViewer`: src, alt, neutral `ViewerAnnotation[]` (original-pixel rectangle, ID, label, appearance, optional per-marker move/resize restrictions), optional controlled selection/mode and callbacks. Ref exposes fit/zoomBy.
- `options`: zoom, pan, selection, draw, move, resize, labels (selected/all/none), showHint, minBoxSize. Capabilities default off. Writing needs explicit permission plus callbacks; disabling zoom also blocks wheel and imperative zoom. Turning capabilities off cancels current gestures. Changing image source resets viewport/loading/gesture state.
- `shared/imaging/comparison/ImageComparison`: panes (`id`, `label`, `content`), controlled view (`both` or pane ID), onView, optional toolbar actions, gallery/workspace layout. Caller owns semantic pairing and shared selection; missing pane selection falls back safely to remaining panes.
- `shared/imaging/viewer/AnnotatedImage`: read-only fit-to-pane preset, no editing/zoom/pan. Both this and the WFP review adapter use the same SVG marker renderer; Results shows all labels, Review shows selected labels and permitted resize handles.

Review keeps persistence, history, twin matching and labels in WFP adapters/hooks. `npm test` includes DOM interaction tests using synthetic image dimensions, plus the existing geometry/history tests. The DOM tests exercise actual shared components; they do not replace full browser/API regression checks.

## Imports and formatting (30 Oct 2026)

- Path aliases (`tsconfig.app.json` "paths" and `vite.config.ts` `resolve.alias`): `@app/*` → `src/app`, `@shared/*` → `src/shared`, `@modules/*` → `src/modules`.
- Use an alias when an import leaves its own area: `import { Button } from '@shared/controls'` inside a module, `import { watchFeralPigs } from '@modules/watch-feral-pigs'` in the app shell. Inside one area keep `./` and `../` (for example `../../api/types` inside WFP).
- The ESLint boundary rules check both spellings: `shared/` may not import `@app`/`@modules` or `../app`/`../modules`; a module may not import `@app` or any `@modules/...` (a module never imports another module).
- Prettier formats the code (`.prettierrc.json`: 120 characters, single quotes, semicolons, trailing commas). `npm run format` rewrites files, `npm run format:check` only checks. Markdown is not formatted.
- Removed unused code: `shared/controls/Banner.tsx`, `modules/watch-feral-pigs/components/EstimateEquation.tsx`, and the unused `Annotation`/`AnnotationTone`/`AnnotationEditorEvents` types (the viewer's record is `ViewerAnnotation`).

## Feature boundaries and server state (30 Oct 2026, docs/18 step 2)

- A feature never imports another feature; ESLint rejects `../<other-feature>/...`. Code two features need goes to module level: `api/queries/` (hooks), `api/keys.ts` (keys), `components/` (UI), `domain/` (rules and labels).
- Every WFP query key is defined in `api/keys.ts` and starts with `'wfp'`. `tests/query-keys.test.mjs` fails if a key is written by hand anywhere else, and checks the refresh prefixes the review relies on.
- `api/types.ts` and `api/client.ts` are barrels over `api/contracts/<feature>.ts` and `api/endpoints/<feature>.ts`, so existing imports keep working; new code can import the feature file directly.
- `features/surveys/hooks.ts` still re-exports `useSurvey`, `useSurveys`, `useSurveyStats` and `surveyKeys` for `features/surveys/photos/` until that folder is moved (docs/18 step 6).

