# Player Profile Slice

Milestone 1.6.1 binds the first Unity Player Profile UI foundation to the shared safe profile pipeline.

## Current State

The profile slice is a development fixture preview only. It clearly says:

- `Fixture Mode`
- `No live FM26 data`
- missing data warnings
- blocked data notes

It uses:

- White/off-white background.
- Frosted glass panels.
- Player initials avatar.
- Flag placeholder.
- Source confidence card.
- Data completeness card.
- Role fit preview.
- Confidence and risk cards.
- Masked evidence and benchmark-unavailable panels.
- Role evidence cards.
- Missing-data warning.

No real player images, club badges or live FM26 claims are used.

## Binding Flow

The safe managed flow is:

```text
synthetic raw fixture
-> ScoutingKnowledgeFirewall
-> MaskedPlayer
-> RoleScoringEngine
-> SourceMetadata
-> DataCompletenessReport
-> MaskedPlayerProfileViewModel
-> visual profile models
-> UnityProfileRenderModel
-> Unity UI
```

The synthetic raw fixture is created privately by `FixtureProfileFactory`. It includes visible development values such as display name, age, nationality, primary position, Finishing, Pace, Acceleration, xG, xA, TopSpeed and SprintDistance. It also includes blocked hidden categories such as CurrentAbility and Professionalism so tests can prove raw hidden values are excluded.

Unity consumes `UnityProfileRenderModel`, a thin render adapter built only from `MaskedPlayerProfileViewModel`. Raw player data must not bind to Unity UI.

Milestone 1.7 also proves that an imported synthetic CSV player can be persisted as safe SQLite data, reloaded as a masked player and used to build `MaskedPlayerProfileViewModel`. Unity still renders the fixture factory profile for now; persistence binding is a later UI task.

## Visual Intelligence

Milestone 1.6 adds UI-ready visual model contracts for masked evidence, unavailable benchmark bars, role fit, confidence, risk, evidence cards, historical-data placeholders, comparison cards, missing data warnings and blocked data notices. Milestone 2.1 keeps benchmark-style output unavailable until a real comparison group exists; the dashboard preview must not present fake radar, percentile or trend claims.

## Safety Rules

- Fixture mode must remain visible.
- No live FM26 data must remain visible.
- FM26 memory maps remain unsupported until validated.
- Blocked-data notices may show counts and categories, not raw hidden values.
- Missing-data warnings must stay visible.
- Player images use initials or a silhouette unless a source explicitly permits player images.
- Provider flags require permission; otherwise only bundled safe placeholder flags may be shown.
- Club badges remain hidden unless permission exists.
- Persisted profiles must be rebuilt from safe stored masked data, not original raw provider snapshots.
