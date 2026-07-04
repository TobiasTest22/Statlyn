# Architecture

Statlyn's target architecture is:

```text
C# Core
= football logic and decisions

React/Tauri UI
= professional analyst workspace

C++ connector
= FM data reader later

SQLite/PostgreSQL
= data storage
```

The existing C# foundation remains the brain of the product. React/Tauri is now the strategic desktop UI. Unity is preserved only as the current legacy/prototype shell. FM26 is one future data source and proof-of-concept environment, not the product foundation.

Statlyn is organized around provider-agnostic football data ingestion and a mandatory field-policy visibility layer.

```text
Data provider
-> mapper
-> normalized football model
-> scouting knowledge firewall and masking
-> analytics/application services
-> safe DTOs
-> React/Tauri UI
```

For FM26, the data provider is the native connector reading the active process with query/read permissions only. For future real-life data, providers must declare their licence status, confidence, completeness and allowed usage.

The FM26 native connector now crosses into managed code through `Statlyn.DataProviders/Fm26/NativeFm26Connector`. That bridge is diagnostics-only: connector availability, version/build info, FM process detection, read-only access status and support state. It intentionally does not expose raw snapshots, player data, native handles, module base addresses or memory addresses.

## Main Projects

- `Statlyn.Core` contains shared football domain types, diagnostics, masked fields and generic tactical future hooks.
- `Statlyn.DataProviders` defines the `IDataProvider` contract and FM26 provider facade.
- `Statlyn.Scouting` owns `ScoutingKnowledgeFirewall`.
- `Statlyn.Analytics` owns C# decision engines and scores only masked players.
- `Statlyn.UI` rejects raw entities at binding boundaries.
- `Statlyn.Data` holds repository interfaces, SQLite implementation, workflow services and safe view models.
- `Statlyn.Api` exposes local safe DTO endpoints for React/Tauri.
- `Statlyn.Desktop` is the strategic React/Tauri desktop workspace.
- `Statlyn.NativeConnector` is the Windows C++ connector-only boundary.
- `Statlyn.UnityApp` is the legacy/current prototype shell.

## Non-Negotiable Boundary

Raw provider entities are allowed only in provider and firewall code. UI and scoring receive `MaskedPlayer` instances or safe DTOs only.

SQLite persistence follows the same boundary. Repositories accept `MaskedPlayer`, `VisiblePlayerField`, `RoleScore`, `SourceMetadata`, `DataCompletenessReport`, blocked-field notices and safe audit models. They reject raw provider entities and skip unknown, blocked or non-storable fields.

The Unity Player Profile fixture uses the same managed boundary as tests:

```text
synthetic raw fixture
-> ScoutingKnowledgeFirewall
-> MaskedPlayer
-> RoleScoringEngine
-> SourceMetadata
-> DataCompletenessReport
-> MaskedPlayerProfileViewModel
-> UnityProfileRenderModel
-> Unity UI Toolkit
```

The Unity adapter is a rendering shape only. It is built from `MaskedPlayerProfileViewModel`, not from `PlayerRawSnapshot`.

React/Tauri must receive only DTOs from `Statlyn.Api`. It must not read SQLite, call provider code, inspect raw FM memory, calculate recruitment scores, calculate role fit, run benchmarks or duplicate firewall rules.

Connector status follows the same rule. React/Tauri calls `/connector/status` through the API client and renders safe DTO fields only. Tauri/Rust code must not perform process inspection or call the native connector.

The current desktop development mode starts `Statlyn.Api` separately from Tauri. Future packaging can introduce a sidecar process once the API contract and desktop validation path are stable. Until then, Tauri remains a thin shell around React assets and local API calls; Rust code must not add database, provider, connector or decision logic.

Unknown fields are denied by default. Provider facts are not trusted simply because they are named `VisibleFacts`.

Grouped fields use `FieldInstanceKey` so values like `TechnicalAttribute:Finishing`, `TechnicalAttribute:Pace`, `PlayerStat:xG`, `PhysicalData:TopSpeed` and `ScoutObservation:PressingEffort` cannot overwrite one another.

## Current Build Support

No FM26 build is validated yet. The app and provider therefore return unsupported diagnostics and empty player snapshots rather than fixture data. The Unity fixture preview is synthetic development data and must remain clearly labelled as no live FM26 data.

## API Contract Direction

Milestone 3.0 keeps the current endpoint contract as direct safe DTOs to avoid destabilizing the new desktop client. A later API-contract milestone can introduce a shared `ApiResponse<T>` envelope with `status`, `data`, `warnings`, `errors`, `safeMessage` and `generatedAtUtc` once the desktop screens and tests are ready to migrate together.

## Performance Output Direction

The persistence layer now includes generic performance metric definitions and role-output expectation profiles. These prepare Statlyn for role-specific output evaluation without claiming FM26 support. Seeded metrics such as xG, xA, progressive passes, tackles, save percentage and physical outputs are generic import/testing definitions until later validated against supported FM26-visible data, exports or memory maps.

Role-output profiles are position-specific: goalkeeper, centre-back, wide attacker, striker and central midfielder templates do not share one universal metric set. Attributes remain supporting evidence rather than the whole recruitment model.
