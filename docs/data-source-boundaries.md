# Data Source Boundaries

All data must follow this route:

```text
Provider/source
-> mapper
-> normalized football model
-> scouting firewall/masking
-> analytics/application service
-> safe DTO
-> React/Tauri UI
```

Forbidden routes:

- provider/source to React
- raw FM data to analytics
- hidden values to DTOs
- raw database rows to frontend
- frontend-calculated recruitment decisions

FM26 is only one possible source. CSV imports, manual club datasets, licensed APIs and other permitted sources can be added later through provider abstractions and mappers.

Unknown fields are denied by default. Hidden CA, hidden PA, hidden personality values, raw blocked values, memory addresses and raw provider dumps must never reach DTOs or UI.
