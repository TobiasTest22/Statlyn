# Scouting Knowledge Firewall

The scouting knowledge firewall is the most important product boundary in Statlyn.

It turns raw provider snapshots into masked entities. Only masked entities may be stored, scored or displayed.

The firewall now delegates field decisions to `FieldPolicyRegistry` and `FieldVisibilityEvaluator`.

## FM26 Rules

- Hidden CA is never displayed, stored or scored.
- Hidden PA is never displayed, stored or scored.
- Hidden personality and hidden mental values are never displayed, stored or scored.
- Mislabeled hidden fields inside visible facts are blocked.
- Visible attributes are displayed only when the user's in-game knowledge allows it.
- Low scouting knowledge lowers confidence and can force a `ScoutFurther` recommendation.
- Unknown values remain unknown; they are not inferred from hidden memory.

## Current Implementation

`Statlyn.Scouting.ScoutingKnowledgeFirewall` accepts `PlayerRawSnapshot` and returns `MaskedPlayer`.

`Statlyn.Analytics.RoleScoringEngine` rejects raw entity input.

`Statlyn.UI.BindingPolicy` rejects raw entity binding.

Blocked fields become audit notices, not usable values.

## Covered Tests

- Hidden CA in raw fixture never appears in masked player.
- Hidden PA in raw fixture never appears in masked player.
- Hidden personality values never appear in masked player.
- Unknown attributes reduce confidence.
- Low scout knowledge prevents automatic `Sign`.
- UI cannot bind raw player entities.
- Scoring rejects raw entities.
- Unsupported FM26 builds return no fake players.
