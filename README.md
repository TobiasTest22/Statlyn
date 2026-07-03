# Statlyn

Statlyn is a desktop football recruitment intelligence platform for Football Manager 26 and future permitted football data sources. The first target is a Unity desktop app that can connect to a local FM26 process through a read-only native connector, then pass every raw field through a scouting knowledge firewall before analytics or UI can use it.

## What Statlyn Is

- A recruitment analysis workspace for squad planning, player comparison, scouting workflows and role-fit decisions.
- A provider-agnostic platform intended to support FM26 live memory, CSV imports, manual club datasets and licensed APIs.
- A white, glassy desktop UI foundation for a serious recruitment department style product.

## What Statlyn Is Not

- Not a cheat tool.
- Not a save editor.
- Not an FM memory writer.
- Not a hidden CA/PA viewer.
- Not a scraper for unlicensed real-life football data.
- Not a fake dashboard with placeholder players pretending to be live data.

## Current Status

- Unity shell: initial desktop UI shell and diagnostics surface created.
- Core C# libraries: provider model, masked player model, diagnostics, scoring guardrails and scouting knowledge firewall created.
- Native connector: C++ read-only process-detection skeleton created.
- FM26 build support: no validated memory maps yet.
- External provider support: CSV/JSON/provider skeletons only.
- Persistence: local SQLite foundation can initialize, import synthetic CSV fixture data, persist masked/source-tagged records, reload safe player data and build a profile view model.
- Data Sources UI: first Unity page for local CSV path entry, source permissions, read-only column preview, safe import and database diagnostics.
- Unity runtime validation: Data Sources includes a runtime check for managed assemblies, SQLite dependencies, temporary database initialization and workflow construction.
- Field policy registry: deny-by-default masking for display, scoring and storage.
- Field instance keys: grouped values such as `TechnicalAttribute:Finishing` and `PlayerStat:xG` are preserved without overwriting.
- Player Profile: fixture preview flows through `ScoutingKnowledgeFirewall`, `RoleScoringEngine` and `MaskedPlayerProfileViewModel` before Unity renders it.
- Performance output foundation: generic/import-ready metric definitions and role-output expectation templates are seeded for future role-specific scoring.

When FM26 is detected but no validated memory map exists, Statlyn must show an unsupported or partial state and return no fake player data.

CSV and JSON import support is local-file skeleton work only. The Data Sources workflow is CSV-only for now: preview reads headers/counts without storing data, then safe import runs through the provider, scouting firewall and SQLite transaction path. It does not scrape, call FotMob, use unofficial endpoints or assume unlicensed images are available. Fixture data is synthetic development/test data only. The Player Profile slice is generated from a synthetic raw fixture that passes through the scouting firewall, role scoring and masked profile view model. It is not live FM26 data.

The SQLite persistence layer is local-only foundation work. It stores masked players, visible permitted fields, player stats, physical metrics, source metadata, role scores, blocked-field audit metadata and import audit counts. Imports run inside a transaction and re-imports replace the current stored player snapshot so safe fields, stats and metrics do not duplicate. It does not store raw provider snapshots, hidden FM26 values or raw blocked values.

Performance metric definitions are generic/import-ready contracts, not official FM26 stat declarations. A metric can only be marked FM26-supported after later validation from visible FM26 data, exported data or a validated memory map. Generic role-output expectation profiles are foundation templates, not final FM26 role templates. Goalkeepers, centre-backs, midfielders, wide attackers and strikers intentionally use different output expectations.

## Repository Layout

- `Statlyn.UnityApp` - Unity desktop application shell.
- `Statlyn.NativeConnector` - C++ Windows read-only connector skeleton.
- `Statlyn.Core` - shared domain models, masked fields and diagnostics.
- `Statlyn.Data` - local database schema contracts.
- `Statlyn.DataProviders` - provider abstraction and FM26 provider facade.
- `Statlyn.Analytics` - role scoring and verdict guardrails.
- `Statlyn.Scouting` - scouting knowledge firewall.
- `Statlyn.UI` - bindable view-model safeguards.
- `Statlyn.Tests` - automated tests for masking and scoring safety.
- `memory-maps` - FM26 memory-map registry templates.
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

Build the native connector with CMake and a Windows C++ toolchain:

```powershell
cmake -S Statlyn.NativeConnector -B build\native
cmake --build build\native --config Release
```

Open `Statlyn.UnityApp` with Unity 6 or newer to run the desktop shell.

Before opening Unity, copy the shared managed assemblies, SQLite dependencies and synthetic fixture CSV into Unity folders:

```powershell
.\tools\copy-managed-to-unity.ps1
```

Unity editor validation is still manual unless a release note says it was opened and checked locally. SQLite is verified by managed tests; use the Data Sources page's `Run Runtime Check` button in Unity to validate SQLite loading in the Editor before relying on CSV import there.

GitHub Actions validates the managed build/tests and the native CMake build.

## Core Safety Rule

Raw FM26 data may not bind to UI and may not enter scoring. The required flow is:

```text
raw provider data -> scouting knowledge firewall -> masked player -> scoring/UI
```

Hidden CA, hidden PA and hidden personality values are blocked by design and covered by tests.

Attributes are supporting evidence for recruitment analysis. Future scoring should increasingly be driven by performance output, role-specific statistical evidence, scout observations, source confidence, sample size and tactical fit.

Import audit diagnostics are sanitized before storage. Role score recommendations are persisted as stored decisions rather than recalculated on reload, and player-stat sample minutes are stored when a safe Minutes field is available.
