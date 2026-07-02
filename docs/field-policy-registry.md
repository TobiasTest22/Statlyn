# Field Policy Registry

The field policy registry is the deny-by-default gate between raw provider data and masked Statlyn data.

## Core Types

- `PlayerFieldKey`: strongly typed field identifiers.
- `FieldPolicy`: display, scoring, storage, scouting and licence rules for one field.
- `FieldPolicyRegistry`: default policies and raw-name resolution.
- `FieldVisibilityEvaluator`: evaluates a raw field with source and scout context.
- `SourceContext`: provider, licence, image, flag and confidence metadata.
- `ScoutContext`: managed-club, scout report and scout knowledge metadata.
- `RawFieldValue`: one provider field before masking.

## Rules

- Unknown fields are blocked.
- Hidden FM26 values are blocked.
- Mislabeled forbidden values in `VisibleFacts` are blocked.
- Source-licensed fields are blocked if the source is not licensed.
- Player images are blocked unless the source permits display.
- Nationality flags require source permission or bundled safe flag assets.

Forbidden raw names include ability values, hidden personality, injury proneness, consistency, important matches, professionalism, pressure, ambition, loyalty, adaptability and temperament.

Blocked fields become audit notices. Their raw values are not passed to UI, scoring or storage.
