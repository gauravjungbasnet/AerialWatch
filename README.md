# Aerial Watch · Source code

Plan drone surveys of feral pigs and turn raw counts into corrected population estimates, using CDU research
(Hvala, Rogers, Alazab & Campbell 2023). Architecture: see `docs/06-architecture.md`.

```
src/
  backend/                         .NET 8, layered
    AerialWatch.Domain/            Research model + entities (no dependencies)
    AerialWatch.Contracts/         Service interfaces + request/response DTOs the API calls
    AerialWatch.Application/       Business logic: services, validation, repository interfaces
    AerialWatch.Infrastructure/    SQL Server (Persistence/: DbContext, migrations, repositories), clock; later files/AI
    AerialWatch.Api/               Controllers, auth, error handling; Program.cs wires everything
      (inside each: Common/ + Wfp/<Feature>/ — see docs/06-architecture.md §2)
    AerialWatch.UnitTests/         xUnit, no database (81 tests, incl. 9 architecture rules)
    AerialWatch.IntegrationTests/  xUnit against real SQL Server (4 tests)
  frontend/                        React 18 + TypeScript + Vite + Tailwind CSS v4 (Windmill design)
  ml/                              Python FastAPI detection service + training tools (see ml/README.md)
```

## Prerequisites
.NET 8 SDK · Node.js 20+ · Microsoft SQL Server (local default instance `.`; LocalDB or Express also work — change `Server=` in appsettings.json) · (later) Python 3.10+

## Run

### 1. Backend: http://localhost:5080 (Swagger at /swagger)
**Visual Studio:** open `src/backend/AerialWatch.sln`, set **AerialWatch.Api** as the startup project, then choose a profile:
- **AerialWebAPI**: uses the local SQL Server default instance (`Server=.`), database `AerialWatch`. SQL Server is required; there is no in-memory mode.
- **The API does not create or migrate the database.** Create/upgrade it yourself with the EF commands below before
  running (and after pulling a new migration).

**Command line:**
```powershell
cd src\backend
dotnet tool install --global dotnet-ef --version 8.0.10      # once
dotnet ef database update --project AerialWatch.Infrastructure --startup-project AerialWatch.Api   # creates the database + tables + published coefficients
dotnet test                                    # unit tests + 4 SQL Server integration tests (local SQL Server)
dotnet test AerialWatch.UnitTests              # unit tests only (no database needed)
dotnet run --project AerialWatch.Api --launch-profile AerialWebAPI
```
Check: http://localhost:5080/health shows `{"status":"ok","database":"SqlServer","databaseConnected":true}`.
Integration tests create and drop their own `AerialWatch_Test_…` databases; set `AERIALWATCH_TEST_SQL_SERVER` to use
another server (e.g. `localhost\SQLEXPRESS`).

Change the connection string in `AerialWatch.Api/appsettings.json` → `ConnectionStrings:AerialWatch` if you use SQL Express.

### 2. Frontend: http://localhost:5173
```powershell
cd src\frontend
npm install
npm run dev
```
Vite proxies `/api` and `/health` to port 5080. The top bar includes a grid app launcher: WFP (Watch Feral Pigs), WWL (Watch Wetlands), and WGN (Watch Ghost Net). Full names appear on hover or keyboard focus; WWL and WGN are coming soon.

### 3. Detection service (optional until the AI step is wired in): http://localhost:8000
```powershell
cd src\ml
python -m venv .venv; .venv\Scripts\activate
pip install -r requirements-dev.txt
python -m pytest -q tests                        # 13 tests
uvicorn aerialwatch_ml.service:app --port 8000   # no weights yet -> hotspot baseline
```
Training and evaluation: `src/ml/README.md` and `docs/09-machine-learning-plan.md`.

## Status
| Slice | Status |
|---|---|
| 1. Research model + Planner | ✅ |
| 1b. Layered backend + daisyUI frontend + surveys (create/list/detail/delete) | ✅ |
| 1c. Navbar app launcher (search and API status removed) | ✅ |
| 1d. App modules + shared controls/navigation structure | ✅ |
| 1e. SQL Server only: migrations, repositories, coefficients in DB with history | ✅ |
| 2. Photo upload + EXIF + file storage | ✅ |
| 3. Review tool (D-034) | ✅ (apply migration `AddReviewFields`) |
| 4. AI detection: Python service, baseline, training tools (D-035) | 🟡 service + tools done; training on BAMBI and .NET client next |
| 5. Results + map | ⏳ |


App launcher refinement: active WFP tile has a border; future-app availability is in tooltips; enlarged nine-dot trigger.

Current app identity: Watch Feral Pigs appears beneath the sidebar brand and in the mobile navbar.

Dashboard simplified: removed the How it works card; visibility chart fills the row.

Frontend folder guide: [frontend/README.md](frontend/README.md). WFP views, routes, navigation and API contracts are inside modules/watch-feral-pigs; reusable controls and helpers are in shared; the platform shell is in app.

Module routing: / redirects to /wfp; /wwl and /wgn open their coming-soon views. APIs follow /api/{module}/{controller}/{method}; controllers inherit an authenticated base, with Development-only local bypass and Admin protection for model parameters. Restart the backend for changed routes. See frontend/README.md and docs/06-architecture.md.

Upload UI fix (26 Oct 2026): first-batch failures and cancellation messages remain visible even when there are no per-file results. See evidence/2026-10-26-upload-ui-error/README.md.

Survey detail (26 Oct 2026): Notes and checks opens in a popup from the info button beside Delete survey; photo content uses the full width.

Research navigation (26 Oct 2026): the active module's header navigation supplies the info icon beside dark mode; WFP opens About the research. NavItem placement defaults to sidebar.

AI training page (28 Oct 2026): corrected shared shell height and grid sizing to avoid duplicate page scrollbars. See evidence/2026-10-28-ai-page-scroll/README.md.

Survey detail scrollbar (28 Oct 2026): document height and overflow now prevent the extra browser scrollbar while main retains page scrolling.

Results UI (28 Oct 2026): shorter result summary, expandable AI and calculation details, paginated photo evidence, compact missing-GPS message. Evidence: evidence/2026-10-28-results-ui/.

Map-first results (29 Oct 2026): implemented reference layout with responsive panels, live map/photo selection and accessible information dialog. See evidence/2026-10-29-results-map-ui/.

Results interaction (29 Oct 2026): wheel/pinch map zoom and on-demand photo bottom sheet, with single-photo marker filtering. Validation: evidence/2026-10-29-results-photo-sheet/.

Review drawing shortcuts (29 Oct 2026): Ctrl/Cmd+drag temporarily draws; Ctrl/Cmd+Z/Y undo/redo new ranger boxes per photo in the current review session. Existing N and U shortcuts remain. Tests/evidence: evidence/2026-10-29-review-drawing-shortcuts/.

Review box labels (29 Oct 2026): number/status captions appear only on the selected box; unselected boxes retain their borders.

Review layout (29 Oct 2026): implemented supplied reference using real data and current controls; separate camera tabs and thumbnail navigation, responsive twin panes, refined sidebar/progress. Evidence: evidence/2026-10-29-review-reference-ui/.


Frontend organisation (29 Oct 2026): integrated shared annotation geometry, separated training contracts from UI exports, typed module route layout metadata, and moved camera presentation helpers to WFP domain/. Compatibility exports retain existing imports. See frontend/README.md and evidence/2026-10-29-frontend-foundations/.

Dashboard design (29 Oct 2026): applied the supplied reference to dashboard content only: four metric cards, a searchable recent-surveys panel, and visibility card. Existing shell/navigation and survey workflows retained. Evidence: evidence/2026-10-29-dashboard-design/.

App typography (29 Oct 2026): Segoe UI is the global sans-serif font. Removed the Inter download; devices without Segoe UI use their system sans-serif fallback.

Dashboard tiles (29 Oct 2026): use Material Design Icons from @mdi/js through shared MdiIcon. Other app icons remain unchanged.

Dashboard layout (29 Oct 2026): recent surveys and visibility today sit side by side on wide screens (2:1 columns), stacked below the xl breakpoint.

Dashboard panel heights (29 Oct 2026): adjacent survey/visibility cards stretch to the taller content, including loading, empty and populated survey states.

Results legend (29 Oct 2026): aligned confirmed/none marker columns and removed camera-location/photo-order caption at user request.

Dashboard empty-state spacing (29 Oct 2026): recent-surveys content fills the equal-height card, centres the empty message, and keeps the footer flush to the bottom.

Review navigation (29 Oct 2026): completed reviews show a View results button in the review page header, linking directly to that survey’s results.

Photo evidence bottom sheet (29 Oct 2026): reference-inspired image-first layout with camera tabs, checked/count bar, larger paired images, caption labels and expandable findings. Retains actual marks and review navigation.

Photo info and shared display viewer (29 Oct 2026): an info button beside the pig count opens the shared Modal. AnnotatedImage in shared/imaging/viewer is reusable across modules, with fit-to-pane display and no zoom. Editable review workflow remains module-owned.

Shared imaging integration (30 Oct 2026): Review and Results use the same configurable ImageViewer, SVG rectangle overlay and ImageComparison. Review enables pan/zoom/selection/editing; completed reviews disable writes. Results uses the read-only preset with zoom off. Added DOM interaction coverage for these configurations. See frontend/README.md and evidence/2026-10-29-shared-imaging/.
