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
- External provider support: interface foundation only.

When FM26 is detected but no validated memory map exists, Statlyn must show an unsupported or partial state and return no fake player data.

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

## Core Safety Rule

Raw FM26 data may not bind to UI and may not enter scoring. The required flow is:

```text
raw provider data -> scouting knowledge firewall -> masked player -> scoring/UI
```

Hidden CA, hidden PA and hidden personality values are blocked by design and covered by tests.
