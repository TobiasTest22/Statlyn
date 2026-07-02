# FM26 Memory Maps

This registry is empty by design until a specific Football Manager 26 build is validated.

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
