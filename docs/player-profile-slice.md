# Player Profile Slice

Milestone 1.5 adds the first Unity Player Profile UI foundation.

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
- Role fit placeholder.
- Confidence and risk cards.
- Radar and percentile placeholders.
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
-> MaskedPlayerProfileViewModel
-> visual profile models
-> Unity UI
```

The Unity shell currently renders from a view-model-shaped fixture preview object while the managed profile contracts are tested in `Statlyn.UI`. Raw player data must not bind to Unity UI.

## Visual Intelligence

Milestone 1.6 adds UI-ready visual model contracts for radar metrics, percentile bars, role fit, confidence, risk, evidence cards, trend placeholders, comparison cards, missing data warnings and blocked data notices. These are placeholders for future chart rendering, not decorative chart output.

## Next Steps

Future milestones should bind this surface to masked player profile view models only. Raw provider entities must never bind to the profile UI.
