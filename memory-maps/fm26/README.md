# FM26 Memory Maps

This registry is metadata-only in Milestone 3.3. The checked-in files under `templates` are templates, not usable Football Manager 26 maps.

Build-specific maps must live under:

```text
memory-maps/fm26/{build}/players.map.json
memory-maps/fm26/{build}/clubs.map.json
memory-maps/fm26/{build}/nations.map.json
memory-maps/fm26/{build}/contracts.map.json
memory-maps/fm26/{build}/tactics.map.json
memory-maps/fm26/{build}/scout_knowledge.map.json
```

Every field must declare whether it can be displayed, scored and stored. Hidden CA, hidden PA and hidden personality values must be `canDisplay: false`, `canScore: false` and `canStore: false`.

Validation rules:

- malformed JSON fails validation
- templates pass syntax/guardrail validation but remain unusable
- unvalidated maps remain unusable
- write-enabled maps fail validation
- hidden fields must be blocked from display, storage and scoring
- API/Desktop receive safe summary counts only
- no raw offsets, addresses, handles or player values are exposed
