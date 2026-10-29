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

For a **Windows development PC**, install:

| Tool | Required for | Check in PowerShell |
|---|---|---|
| **.NET 8 SDK** (not just the runtime) | Backend and EF migrations; projects target `net8.0` | `dotnet --list-sdks` must include an `8.0.xxx` SDK |
| **Node.js 20+ with npm** | Frontend installation, build and dev server | `node --version` and `npm --version` |
| **Microsoft SQL Server Database Engine** | Required application database; default instance, Express or LocalDB | Connect to the installed instance with Windows authentication |
| SQL Server Management Studio (optional) | Inspect databases, run the SQL checks below, backup/restore | Open and connect to the same instance |
| Git (if cloning) | Get/update the source | `git --version` |
| Python **3.11 or 3.12** (optional) | Standalone ML service/tests; not needed for frontend/backend/manual review | `py --list` |

An editor is optional for running the commands. Visual Studio users should install ASP.NET/web development support.
SQL Server Management Studio by itself does **not** install the database engine. PostgreSQL, MySQL and SQLite are not supported here.
Close and reopen PowerShell after installing tools so PATH is refreshed.

## First time on a new PC — read this before Run

### 1. Get the complete source

Clone the repository URL provided by the owner, or extract the source archive into a local folder.
Do not guess the Git remote URL. Example workspace: `C:\Projects\CodingCompetition`.
The commands below assume this structure exists:

- `AGENTS.md` and `docs/` at the workspace root.
- `src/backend/AerialWatch.sln` and the projects it references.
- `src/frontend/package.json` **and** `src/frontend/package-lock.json`.
- `src/backend/AerialWatch.Infrastructure/Persistence/Migrations/` including the snapshot.

Do not copy another PC's `node_modules`, `bin`, `obj` or Python `.venv`; install/restore dependencies locally.
A fresh setup starts with an empty survey list. The source does not include an existing database, uploaded photos, datasets or trained model weights.

### 2. Choose the SQL Server instance and local paths

Open **PowerShell terminal A** in your workspace. Replace the example workspace path with your actual folder:

```powershell
Set-Location 'C:\Projects\CodingCompetition'

# Pick ONE installed database engine instance. This is the repository default:
$env:ConnectionStrings__AerialWatch = 'Server=.;Database=AerialWatch;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True'

# If you installed SQL Server Express instead, use this line INSTEAD:
# $env:ConnectionStrings__AerialWatch = 'Server=.\SQLEXPRESS;Database=AerialWatch;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True'

# If you installed SQL Server Express LocalDB instead, use this line INSTEAD:
# $env:ConnectionStrings__AerialWatch = 'Server=(localdb)\MSSQLLocalDB;Database=AerialWatch;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True'

# Store uploads/logs/exports outside the source, under your Windows account:
$env:Storage__RootPath = Join-Path $env:USERPROFILE 'AerialWatchData'
$env:Training__ExportRoot = Join-Path $env:Storage__RootPath 'training-exports'
New-Item -ItemType Directory -Force -Path $env:Storage__RootPath, $env:Training__ExportRoot | Out-Null
```

Only use a connection-string line for an instance that actually exists. `.` means the local default instance;
`.\SQLEXPRESS` means the named Express instance; `(localdb)\MSSQLLocalDB` means the current user's LocalDB instance.
For LocalDB, `sqllocaldb info` lists installed instances and `sqllocaldb start MSSQLLocalDB` starts that instance if it exists.
For the other installations, ensure the matching SQL Server Windows service is running.

These are **current-terminal** environment variables. Keep terminal A open for migrations and the backend.
Repeat the assignments when opening a new backend/migration terminal. They override JSON settings for both EF and the API;
this is important so you do not migrate one database and run the API against another.
The frontend terminal does not need these variables. A standalone `.env` file is not automatically loaded by the .NET configuration here.

The checked-in JSON defaults are `Server=.` / database `AerialWatch`, `C:\AerialWatchData` for storage and
`C:\AerialWatchData\training-exports` for exports. If these already work for your account, the path overrides are optional.
Alternatively, configure the corresponding `ConnectionStrings:AerialWatch`, `Storage:RootPath`, and `Training:ExportRoot`
settings locally; do not commit passwords or another person's machine paths. `appsettings.local.json` is not automatically loaded.
For SQL authentication or a remote server, get the real connection details from the owner rather than inventing credentials.
Windows-authenticated setup requires your Windows login to have permission to create/upgrade this development database.

### 3. Follow the Run sections in order

In terminal A, run **Backend** below: restore dependencies, apply existing migrations, then start the API.
Use **terminal B** for **Frontend**. The database must be ready before using the app.
For daily use, repeat the terminal-A settings, apply any newly supplied migrations, start the API, then start Vite.
Use Ctrl+C in each server terminal to stop the development servers.


## Run

### 1. Backend: http://localhost:5080 (Swagger at /swagger)
**Visual Studio:** open `src/backend/AerialWatch.sln`, set **AerialWatch.Api** as the startup project, then choose a profile:
- **AerialWebAPI**: uses the local SQL Server default instance (`Server=.`), database `AerialWatch`. SQL Server is required; there is no in-memory mode.
- **The API does not create or migrate the database.** Create/upgrade it yourself with the EF commands below before
  running (and after pulling a new migration).

**Command line:**
```powershell
cd src\backend
dotnet restore AerialWatch.sln                 # downloads NuGet packages; internet required on first setup
dotnet tool install --global dotnet-ef --version 8.0.10      # once; use this EF version with the checked-in 8.0.10 packages
dotnet ef database update --project AerialWatch.Infrastructure --startup-project AerialWatch.Api   # creates the database + tables + published coefficients
dotnet run --project AerialWatch.Api --launch-profile AerialWebAPI
```
If `dotnet-ef` is already installed, inspect `dotnet ef --version` rather than reinstalling it blindly.
Use the project-compatible 8.0.10 tool for this checkout; `dotnet tool update --global dotnet-ef --version 8.0.10`
updates a compatible existing installation. A conflicting global major version should be resolved deliberately, since other projects may use it.

Check [API health](http://localhost:5080/health). The JSON should include `status: "ok"`, `database: "SqlServer"`
and `databaseConnected: true`. HTTP 503 / `degraded` means SQL Server is unreachable; fix that before continuing.
A health pass checks connectivity, not that every migration/table is present. [Swagger](http://localhost:5080/swagger)
is available with the Development launch profile. In another terminal you can check:

```powershell
Invoke-RestMethod 'http://localhost:5080/health'
```

The `AerialWebAPI` profile sets **Development**, where checked-in `Authentication:DevelopmentBypass = true`
provides a local Admin identity. Outside Development the API requires authentication and sign-in is not implemented;
anonymous calls return 401 and denied roles return 403. Keep this setup local; do not enable a bypass outside Development.
Integration tests create and drop their own `AerialWatch_Test_…` databases; they require database-create/drop permissions.
They use server `.` by default and do **not** take their server from `ConnectionStrings__AerialWatch`.
Before running the full suite, set their server explicitly when using Express or LocalDB, for example:

```powershell
$env:AERIALWATCH_TEST_SQL_SERVER = '.\SQLEXPRESS' # or '(localdb)\MSSQLLocalDB', matching YOUR chosen instance
```

Once the test server is configured, run these from `src/backend` in another terminal (or stop the API first):

```powershell
dotnet test AerialWatch.UnitTests              # unit tests only; no database required
dotnet test AerialWatch.sln                    # full suite, including real SQL Server tests
```

The unit-only command does not need SQL Server. Do not claim a test pass unless the command prints a passing summary.

**Visual Studio configuration:** variables set in terminal A are not automatically inherited by an already-open Visual Studio.
Either launch the API from that terminal as shown above, or set the same connection/storage/export values locally in
`AerialWatch.Api/appsettings.Development.json`, retaining its existing logging/authentication settings. EF reads that file too.
Use your local values rather than overwriting the file with another person's settings; do not commit credentials.

### 2. Frontend: http://localhost:5173
```powershell
cd src\frontend
npm ci                         # install the exact lockfile dependencies, including development tools
npm run dev -- --strictPort     # fail clearly if 5173 is occupied
```
Open [Aerial Watch](http://localhost:5173/wfp). These commands are run from the workspace root in terminal B,
not from terminal A's `src/backend` directory. Use `npm.cmd` instead of `npm` if PowerShell blocks the npm script.
Vite proxies `/api` and `/health` to port 5080. The top bar includes a grid app launcher: WFP (Watch Feral Pigs), WWL (Watch Wetlands), and WGN (Watch Ghost Net). Full names appear on hover or keyboard focus; WWL and WGN are coming soon.

### 3. Detection service (optional; standalone): http://localhost:8000
```powershell
cd src\ml
py -3.12 -m venv .venv                          # or py -3.11 if that is the installed supported version
.\.venv\Scripts\python.exe -m pip install -r requirements-dev.txt
.\.venv\Scripts\python.exe -m pytest -q tests
.\.venv\Scripts\python.exe -m uvicorn aerialwatch_ml.service:app --port 8000
```
Training and evaluation: `src/ml/README.md` and `docs/09-machine-learning-plan.md`.

Run this in **terminal C** from the workspace root. Calling the virtual environment's Python directly avoids activation-policy problems.
Read [ML agent rules](ml/AGENTS.md) before ML work and [ML setup](ml/README.md) for weights and inference configuration.
No weights: thermal uses the hotspot baseline; RGB returns 503. YOLO inference additionally needs the dependencies
in `requirements-train.txt` and separately supplied, permitted weights. The checked-in .NET infrastructure has no registered
ML HTTP client yet: starting Python alone does not connect an automatic Run AI workflow. Frontend never calls Python directly.

## Database migrations: first install, updates and verification

Run from **`src/backend`**, in the same configured terminal as the backend:

```powershell
# See which migrations exist and their application status:
dotnet ef migrations list --project AerialWatch.Infrastructure --startup-project AerialWatch.Api

# Apply ALL unapplied checked-in migrations (first setup or after receiving new code):
dotnet ef database update --project AerialWatch.Infrastructure --startup-project AerialWatch.Api
```

This creates a fresh `AerialWatch` database if needed, applies schema/data changes, and seeds the published model coefficients.
You do not need a separate manual CREATE TABLE or seed script. The migration project is **AerialWatch.Infrastructure**,
not any leftover `AerialWatch.Persistence` folder. Existing successful migrations are tracked in `__EFMigrationsHistory`;
running `database update` again applies pending migrations, not duplicate tables or demo surveys.
If updating an existing database, back it up first. Never drop a friend's existing database to fix a setup error.

Optional inspection in SSMS, connected to the **same configured instance and database**:

```sql
USE [AerialWatch];
SELECT [MigrationId] FROM [dbo].[__EFMigrationsHistory] ORDER BY [MigrationId];
SELECT COUNT(*) AS PublishedAndCustomSets FROM [wfp].[ModelCoefficientSets];
SELECT COUNT(*) AS CoefficientValues FROM [wfp].[ModelCoefficientValues];
```

`dbo.__EFMigrationsHistory` is EF's bookkeeping table; application tables use `wfp` or `core`, not dbo.
Migration history should match the migrations shipped with your copy. A fresh database should have seeded coefficient rows,
while surveys/photos remain empty. Health alone does not verify this.

**Developers changing the database schema only:** add a migration after the EF model/configuration changes, review its code,
update `docs/08-database-schema.md`, then apply it manually. Ordinary new-PC setup uses the existing migrations above;
do not generate a new initial migration or remove the snapshot.

```powershell
# Replace DescribeYourChange with a meaningful migration name:
dotnet ef migrations add DescribeYourChange --project AerialWatch.Infrastructure --startup-project AerialWatch.Api --output-dir Persistence/Migrations
```

The API never calls EnsureCreated/Migrate to initialise your application database. Do not add startup auto-migration as a workaround.

## Verify the complete setup

1. Confirm all migrations are applied and the health endpoint reports database connectivity.
2. Open `/wfp`: the dashboard loads (zero surveys is normal on a new database).
3. Open the planner: study windows and model parameters should load from the backend.
4. Create a test survey using your own or synthetic data; upload a permitted test image, review it manually, complete review,
   and use **View results**. Check that estimates include ranges and assumptions. Photos without GPS work but cannot supply map locations.
5. Stop/restart the backend with the same settings: the survey and uploaded photo should still be available.
6. From `src/frontend`, run `npm run lint`, `npm test`, and `npm run build`. From `src/backend`, run the unit tests and,
   with the correct test SQL instance configured, the full `dotnet test AerialWatch.sln` suite. Record actual summaries.

`npm run build` creates `dist`; it does not deploy the API or configure production hosting.
The Vite development proxy belongs to `npm run dev`, so use that for this local setup rather than treating preview as a complete deployment.
Online basemap tiles require internet access; a tile outage does not mean the survey database is missing.

## If you want existing surveys on the new PC

The source archive alone is insufficient. With the owner's permission, transfer a **SQL Server backup** and the corresponding
uploaded-file storage tree, preserving relative file names/paths. Restore into the intended instance/database and point
`Storage:RootPath` at that copied tree. A database without the files gives missing photos; files without the database give no survey records.
Use a compatible SQL Server version for the backup restore and back up any target data before replacing it.
After restore, check/apply any pending migrations from the new code. Model weights are separate from both.
NACAS/Indigenous/restricted imagery is not part of a standard handover: obtain permission first, follow the root/ML agent rules,
and never put it in source control, cloud assistants or screenshots. Start fresh with permitted test images when permission is absent.

## Common setup problems

| Symptom | What to check |
|---|---|
| `dotnet` not found / framework missing | Install the .NET **8 SDK** and reopen the terminal; check `dotnet --list-sdks`. |
| EF tool not found | Install the pinned tool above; reopen PowerShell so `%USERPROFILE%\.dotnet\tools` is on PATH. |
| SQL network/instance/login error | Correct `Server=`, start that engine/service, and check Windows login permissions. SSMS must connect to the exact same instance. |
| `Invalid object name` / missing columns | Apply all pending migrations against the API's actual connection string. Do not regenerate the initial schema. |
| NuGet/npm restore fails | First setup needs access to nuget.org and the configured npm registry; check connectivity/proxy/certificates. Do not silently change dependencies to work around a network error. |
| 401/403 in local app | Use `--launch-profile AerialWebAPI`, confirm Development and the local bypass setting; do not change controller authorization. |
| Upload/access denied | Storage/export folders must be writable by the account running the API. Check both path overrides and restart with the same settings. |
| Frontend loads but API requests fail | Keep backend running at 5080; verify `/health`; check Vite's proxy in `frontend/vite.config.ts`. |
| Port 5080 or 5173 occupied | Stop your older app instance or deliberately coordinate API launch URL, Vite proxy, frontend origin and CORS. Do not silently run one part on a different port. |
| Existing photos unavailable | Check the matching storage tree as well as the database, not just a successful migration. |
| Integration tests fail while unit tests pass | Configure `AERIALWATCH_TEST_SQL_SERVER`; ensure permission to create/drop the test databases. |
| Photos have no basemap / RGB detection unavailable | Check tile connectivity / optional ML weights separately; do not fabricate locations or AI results. |

## Handoff instructions for a coding agent on the new PC

- Read root `AGENTS.md` first, then the linked research/decision/architecture docs. For ML, also read `src/ml/AGENTS.md`.
- Inspect this PC's paths, installed tools, SQL instance, and existing data before changing settings. These examples do not authorise
  copying Gaurav's private folders or using his Windows username. Do not assume this checkout's remote URL or Git metadata exists.
- Follow the sections above in order. Keep the connection/storage overrides in the backend/migration terminal and use separate frontend/optional-ML terminals.
- Apply the **existing** migrations only after checking the target database. Do not drop data, recreate migrations, change coefficients,
  enable startup migrations, substitute another database, or weaken authentication to fix setup problems.
- Preserve existing project structure and local changes. Run commands from their documented working directory.
- Report actual versions, non-sensitive instance/database names, migration status, health result, frontend checks and test summaries.
  Never print credential-bearing connection strings. If blocked, report the failing command and useful error text; do not claim a pass.


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
