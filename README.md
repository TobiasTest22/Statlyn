# Statlyn

Statlyn is a professional football recruitment intelligence platform built around a C# decision brain, a React/Tauri analyst workspace, connector-only native code and local-first storage.

```text
C# Core/Analytics/Scouting/Data = football logic and decisions
React/Tauri UI = professional analyst workspace
C++ connector = FM data reader later
SQLite now / PostgreSQL later = data storage
```

FM26 is one future data source and proof-of-concept environment, not the product foundation. Statlyn is designed so real-life recruitment data can later arrive through CSV imports, manual club datasets, licensed APIs and other permitted sources.

## What Statlyn Is

- A recruitment analysis workspace for squad planning, player comparison, scouting workflows and role-fit decisions.
- A provider-agnostic platform intended to support FM26 live memory, CSV imports, manual club datasets and licensed APIs.
- A flat black/glassy React/Tauri analyst workspace for a serious recruitment department style product.

## What Statlyn Is Not

- Not a cheat tool.
- Not a save editor.
- Not an FM memory writer.
- Not a hidden CA/PA viewer.
- Not a scraper for unlicensed real-life football data.
- Not a fake dashboard with placeholder players pretending to be live data.

## Current Status

- React/Tauri desktop: strategic UI shell created in `Statlyn.Desktop`; it consumes safe DTOs from `Statlyn.Api` and contains no scoring engines.
- React/Tauri cockpit refinement: Scout Room-style professional dark recruitment analyst layout with stable left navigation, compact search/filter foundation, safe KPI/status cards and a large central recruitment board with no persistent insight panel, no gradients and no copied external data.
- Local API bridge: `Statlyn.Api` exposes safe DTO endpoints for dashboard, players, recruitment board, Role Lab, squad gaps, comparisons, scout reports, data sources and diagnostics.
- FM26 connector diagnostics: `Statlyn.Api` exposes `/connector/status`, `/connector/fm26`, `/diagnostics/fm26`, `/diagnostics/fm26/summary`, `/diagnostics/memory-maps` and `/connector/memory-maps` through safe C# boundaries. These endpoints report connector availability, Windows/platform state, FM process detection, safe process labels, read-only access status, version/build metadata where available, memory-map registry status and next action. FM process detection and map selection are diagnostics only. No player data is read; the endpoints do not expose memory addresses, raw offsets, raw snapshots, hidden values, CA or PA.
- Unity shell: current prototype/legacy desktop shell and diagnostics surface preserved.
- Core C# libraries: provider model, masked player model, diagnostics, scoring guardrails and scouting knowledge firewall created.
- Native connector: C++ read-only process-detection skeleton and managed C# diagnostics binding created.
- FM26 build support: memory-map templates can be loaded as metadata, but no real FM26 player reading exists yet.
- External provider support: CSV/JSON/provider skeletons only.
- Persistence: local SQLite foundation can initialize, import synthetic CSV fixture data, persist masked/source-tagged records, reload safe player data and build a profile view model.
- Data Sources UI: first Unity page for local CSV path entry, source permissions, read-only column preview, safe import and database diagnostics.
- Unity runtime validation: Data Sources includes a runtime check for managed assemblies, SQLite dependencies, temporary database initialization and workflow construction.
- Recruitment Centre: first persisted-safe player list for imported CSV players with search/filter, role/output summaries and full safe profile report loading.
- Player Profile v1: persisted-safe, output-first recruitment report with role-specific output, reusable visual analytics components, data quality, scout actions, blocked-data notice and real benchmark status when a valid comparison group exists.
- Shortlists v1: persisted-safe recruitment decision workflow for creating shortlists, adding players from Recruitment Centre or Player Profile, updating status/priority/follow-up and removing players.
- Scout Desk v1: persisted local human scouting workflow for creating assignments, submitting qualitative reports, answering role/output prompts and optionally updating shortlist status from a scout recommendation.
- Role Lab v1: editable phase-aware role templates, role pairs, output metric requirements, scout questions and red flags; generic/import-safe until FM26 validation exists.
- Benchmark Foundation v1: generic/import benchmark definitions, aggregate-only benchmark runs and nullable percentiles from persisted safe SQLite comparison groups.
- Unity runtime validation v1: Diagnostics page, Runtime Check and Full Smoke Test covering the CSV-only workflow against a separate smoke-test database.
- UI Command-Center Theme Baseline: dark Unity shell, command-center status/KPI helpers, safe local dashboard overview counts and consistent command headers across built and placeholder pages.
- Local CSV release-candidate hardening: product readiness checks, CSV hardening messages, safe database backup/smoke reset foundation and safe snapshot text outputs.
- Branding: official Statlyn logo assets are copied into Unity `Resources/Branding`, packaged under `Statlyn.Desktop/public/branding` and used by the desktop shell/sidebar.
- Field policy registry: deny-by-default masking for display, scoring and storage.
- Field instance keys: grouped values such as `TechnicalAttribute:Finishing` and `PlayerStat:xG` are preserved without overwriting.
- Dashboard: local SQLite overview cards show real safe counts or `Awaiting local data.`; Player Profile v1 loads persisted safe imported data through `PlayerProfileQueryService`, `PlayerProfileReportViewModel` and safe `StatlynVisualAnalytics*` models.
- Performance output foundation: generic/import-ready metric definitions and role-output expectation templates are seeded for future role-specific scoring.

FM26 remains unsupported for player reading. Milestone 3.3 adds a metadata-only memory-map registry loader and validator: templates are not usable, unvalidated maps are not usable, and a validated map only means map metadata is available. First safe player snapshot is later.

CSV and JSON import support is local-file skeleton work only. The Data Sources workflow is CSV-only for now: preview reads headers/counts without storing data, then safe import runs through the provider, scouting firewall and SQLite transaction path. It does not scrape, call FotMob, use unofficial endpoints or assume unlicensed images are available. Fixture data is synthetic development/test data only and is used by tests and explicit fixture helpers, not as fake live dashboard data.

Recruitment Centre, Player Profile, Shortlists, Scout Desk, Role Lab and Benchmarks use persisted safe SQLite data only. They do not query live FM26, external APIs or raw provider snapshots. Latest role-score names are persisted and reloaded from SQLite; missing scores display `Not scored`. Role/output summaries prefer selected Role Lab roles or persisted role-output expectation profiles when available and fall back to generic/import-ready profiles only when needed. Attributes are supporting evidence rather than the whole recruitment model. Recruitment Centre cards now use safe mini visuals for role fit, confidence, completeness, risk, output, benchmark status, missing-data and blocked-field badges. Shortlists store workflow labels such as status, priority and follow-up action; they are not FM hidden values. Scout reports store qualitative human observations and redact hidden-value-looking numeric assignments such as exact CA, PA or hidden personality values before persistence.

The SQLite persistence layer is local-only foundation work. It stores masked players, visible permitted fields, player stats, physical metrics, source metadata, role scores, shortlist workflow rows, scout assignments, qualitative scout reports, Role Lab templates, benchmark definitions, aggregate benchmark snapshots, blocked-field audit metadata and import audit counts. Imports run inside a transaction and re-imports replace the current stored player snapshot so safe fields, stats and metrics do not duplicate. It does not store raw provider snapshots, hidden FM26 values, raw blocked values or selected player raw benchmark values.

Performance metric definitions are generic/import-ready contracts, not official FM26 stat declarations. A metric can only be marked FM26-supported after later validation from visible FM26 data, exported data or a validated memory map. Generic role-output expectation profiles and benchmark definitions are foundation templates, not final FM26 role templates. Goalkeepers, centre-backs, midfielders, wide attackers and strikers intentionally use different output expectations. No fake benchmark percentiles are generated; if no comparison group exists, Player Profile v1 and Recruitment Centre rows say no benchmark yet.

Official logo usage is documented in `docs/branding.md`. Command-center UI guidance is documented in `docs/command-center-ui.md`. React/Tauri desktop usage and the professional dark football recruitment analyst cockpit direction are documented in `docs/react-tauri-ui.md` and `docs/ui-design.md`. FM26 connector diagnostics are documented in `docs/fm26-connector-diagnostics.md`. NPM audit status is documented in `docs/npm-audit-notes.md`. Local CSV release-candidate flow is documented in `docs/local-csv-release-candidate.md`, and database maintenance safety is documented in `docs/database-maintenance.md`. Visual analytics components are documented in `docs/visual-analytics-components.md`. Benchmarks are documented in `docs/benchmarks.md`. Shortlists are documented in `docs/shortlists.md`. Scout Desk is documented in `docs/scout-desk.md` and report safety in `docs/scout-report-safety.md`. Role Lab is documented in `docs/role-lab.md` and the phase-role model in `docs/fm26-phase-role-model.md`. The currently available repo assets include `StatLyn_Logo.png`, `StatLyn_Logo_Reversed.png`, `StatLyn_Mark_White_Tight.png`, `StatLyn_Transparant_Black.png`, `Statlyn_Logo_Black-text.png` and `Statlyn_Logo_White-text.png`.

## Repository Layout

- `Statlyn.Core` - shared football domain models, tactical future hooks, masked fields and diagnostics.
- `Statlyn.Analytics` - C# football decision engines: role fit, recruitment, benchmarks, outperformance, squad gaps, comparisons and red flags.
- `Statlyn.Scouting` - scouting firewall, masking and safe evidence rules.
- `Statlyn.Data` - repositories, SQLite implementation, workflow services and safe view models.
- `Statlyn.DataProviders` - provider abstractions and CSV/manual/FM/future provider mappers.
- `Statlyn.Api` - local ASP.NET Core API bridge for React/Tauri safe DTOs.
- `Statlyn.Desktop` - React + Tauri strategic desktop analyst workspace.
- `Statlyn.NativeConnector` - C++ Windows read-only connector skeleton.
- `Statlyn.UI` - reusable C# safe view-model helpers retained for current shells.
- `Statlyn.Tests` - automated tests for masking and scoring safety.
- `Statlyn.UnityApp` - legacy/current Unity prototype shell only.
- `memory-maps` - FM26 memory-map registry templates and future validated metadata.
- `docs` - architecture, connector, UI and testing notes.
- `tools` - local validation scripts.

## Field Policy Registry

Every raw field must pass through the field policy registry before storage, scoring or UI. Unknown fields are denied by default. Hidden FM26 fields, mislabeled ability values and hidden personality-style values are blocked even if they arrive inside a provider's visible facts.

Grouped football fields are keyed by field instance, not only by broad field category. This allows Statlyn to preserve multiple attributes, stats, physical metrics and scout observations for the same player.

## Build

Build the managed foundation:

```powershell
dotnet build Statlyn.sln
```

Run tests:

```powershell
dotnet test Statlyn.sln
```

Check the native connector source for forbidden process-modification calls:

```powershell
.\tools\check-native-readonly.ps1
```

Run the local desktop validation path:

```powershell
.\tools\run-desktop-validation.ps1
```

This script builds/tests C#, runs the native read-only scan, validates tracked JSON, starts `Statlyn.Api` temporarily, checks `/health`, runs desktop checks and can build the Tauri installer. It does not require Unity or FM26 and stops the temporary API process before exiting.

Run the connector diagnostics path:

```powershell
.\tools\run-connector-diagnostics.ps1
```

This script builds C#, runs the native read-only scan, validates memory-map metadata, starts the local API temporarily, checks `/health`, `/connector/status`, `/diagnostics/fm26` and `/diagnostics/memory-maps`, prints connector availability, process detection, read-only access, support status, map registry status and next action, then stops the API. It verifies that FM26 player reading remains unsupported.

Build the native connector with CMake and a Windows C++ toolchain:

```powershell
cmake -S Statlyn.NativeConnector -B build\native
cmake --build build\native --config Release
```

Open `Statlyn.UnityApp` with Unity 6 or newer to run the desktop shell.

Run the local C# API:

```powershell
dotnet run --project .\Statlyn.Api\Statlyn.Api.csproj --urls http://localhost:5118
```

Run the strategic React/Tauri workspace:

```powershell
cd Statlyn.Desktop
npm install
npm run dev
npm run check
npm run build
npm run tauri:dev
npm run tauri:build
```

The React app calls `Statlyn.Api` through safe DTO endpoints. React/Tauri never calls native connector directly. It does not open SQLite directly, inspect local processes, read FM memory or calculate recruitment, role, benchmark, scouting or provider decisions in TypeScript. In current development mode the API must be started separately; API sidecar bundling is a future packaging milestone.

Tauri installer outputs are produced under:

```text
Statlyn.Desktop\src-tauri\target\release\bundle\msi\
Statlyn.Desktop\src-tauri\target\release\bundle\nsis\
```

Current desktop limitations: FM26 is unsupported, no real data appears unless safe local data is imported into SQLite, and the NPM audit findings are development-tooling findings documented in `docs/npm-audit-notes.md`.

Before opening Unity, copy the shared managed assemblies, SQLite dependencies and synthetic fixture CSV into Unity folders:

```powershell
.\tools\copy-managed-to-unity.ps1
```

Unity editor validation is still manual unless a release note says it was opened and checked locally. SQLite is verified by managed tests; use the Diagnostics page's `Run Runtime Check` and `Run Full Smoke Test` buttons in Unity to validate SQLite loading and the local CSV workflow in the Editor before relying on runtime import there.

For a local CSV release-candidate demo, also run `Run Product Readiness Check`, use `Backup Main Database` before destructive experiments, and use `Reset Smoke-Test Database` only for the separate smoke-test database.

GitHub Actions validates the managed build/tests and the native CMake build.

## Core Safety Rule

Raw FM26 data may not bind to UI and may not enter scoring. The required flow is:

```text
raw provider data -> scouting knowledge firewall -> masked player -> scoring/UI
```

Hidden CA, hidden PA and hidden personality values are blocked by design and covered by tests. Scout report notes are allowed to be qualitative, but hidden-looking numeric assignments are redacted before storage.

Attributes are supporting evidence for recruitment analysis. Future scoring should increasingly be driven by performance output, role-specific statistical evidence, scout observations, source confidence, sample size and tactical fit.

Import audit diagnostics are sanitized before storage. Role score recommendations are persisted as stored decisions rather than recalculated on reload, and player-stat sample minutes are stored when a safe Minutes field is available.
