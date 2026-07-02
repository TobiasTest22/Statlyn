# Field Policy Registry

The field policy registry is the deny-by-default gate between raw provider data and masked Statlyn data.

## Core Types

- `PlayerFieldKey`: strongly typed field identifiers.
- `FieldPolicy`: display, scoring, storage, scouting and licence rules for one field.
- `FieldPolicyRegistry`: default policies and raw-name resolution.
- `FieldVisibilityEvaluator`: evaluates a raw field with source and scout context.
- `SourceContext`: provider, licence, player image, provider flag, bundled safe flag, club badge, export and confidence metadata.
- `ScoutContext`: managed-club, scout report and scout knowledge metadata.
- `RawFieldValue`: one provider field before masking.
- `FieldInstanceKey`: immutable identity for grouped fields.

## Rules

- Unknown fields are blocked.
- Hidden FM26 values are blocked.
- Mislabeled forbidden values in `VisibleFacts` are blocked.
- Source-licensed fields are blocked if the source is not licensed.
- Player images are blocked unless the source permits display.
- Nationality flags require source permission or bundled safe flag assets.
- Club badges are blocked unless the source explicitly permits club badge display.
- Export is a source permission and does not imply image, flag or badge display rights.

Forbidden raw names include ability values, hidden personality, injury proneness, consistency, important matches, professionalism, pressure, ambition, loyalty, adaptability and temperament.

Blocked fields become audit notices. Their raw values are not passed to UI, scoring or storage. The database audit contract stores source name, source entity id, field key, field name, reason and creation time only.

## Field Instance Keys

Statlyn does not key grouped data only by `PlayerFieldKey`. A player can have many technical attributes, player stats, physical metrics and scout observations. Each field therefore carries:

- `PlayerFieldKey`
- `FieldName`
- `SourceFieldName`

Examples:

- `TechnicalAttribute:Finishing`
- `TechnicalAttribute:Pace`
- `PlayerStat:xG`
- `PlayerStat:xA`
- `PhysicalData:TopSpeed`
- `ScoutObservation:PressingEffort`

This prevents safe fields from overwriting one another while keeping broad policy categories intact.

## Mapping Catalog

`FootballFieldCatalog` maps common football CSV columns into safe field instances. Explicit mappings can override catalog mappings for safe fields, but forbidden raw names such as `CurrentAbility`, `CA`, `PotentialAbility` and `Professionalism` always resolve to forbidden fields.
