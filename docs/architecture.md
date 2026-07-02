# Architecture

Statlyn is organized around provider-agnostic football data ingestion and a mandatory visibility layer.

```text
Data provider
-> raw football data
-> source validation
-> licence/permission check
-> scouting knowledge firewall
-> masked Statlyn entities
-> local database
-> analytics and scoring
-> Unity UI
```

For FM26, the data provider is the native connector reading the active process with query/read permissions only. For future real-life data, providers must declare their licence status, confidence, completeness and allowed usage.

## Main Projects

- `Statlyn.Core` contains shared data types, diagnostics and masked fields.
- `Statlyn.DataProviders` defines the `IDataProvider` contract and FM26 provider facade.
- `Statlyn.Scouting` owns `ScoutingKnowledgeFirewall`.
- `Statlyn.Analytics` scores only masked players.
- `Statlyn.UI` rejects raw entities at binding boundaries.
- `Statlyn.Data` holds SQLite schema contracts for visible and source-tagged data.
- `Statlyn.NativeConnector` is the Windows C++ connector.
- `Statlyn.UnityApp` is the desktop frontend.

## Non-Negotiable Boundary

Raw provider entities are allowed only in provider and firewall code. UI and scoring receive `MaskedPlayer` instances only.

## Current Build Support

No FM26 build is validated yet. The app and provider therefore return unsupported diagnostics and empty player snapshots rather than fixture data.
