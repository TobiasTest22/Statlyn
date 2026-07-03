# Import Pipeline

The 1.7 import pipeline proves that a synthetic CSV fixture can pass through the safe Statlyn path and land in SQLite.

```text
IDataProvider.ValidateAccess
-> ReadSourceMetadata
-> ReadPlayers
-> ScoutingKnowledgeFirewall.Mask
-> RoleScoringEngine.ScorePlayer
-> DataSourceRepository
-> PlayerRepository
-> VisibleFieldRepository
-> PlayerStatRepository
-> PhysicalMetricRepository
-> RoleScoreRepository
-> BlockedFieldAuditRepository
-> ImportAuditRepository
```

## Audit Counts

`ImportAudit` tracks:

- source name
- provider type
- import time
- rows read, accepted and rejected
- visible fields stored
- player stats stored
- physical metrics stored
- blocked fields
- unknown fields
- safe diagnostics

Diagnostics may include field names and counts, but not hidden raw values.

## Performance Output

Imported output metrics are preserved as output data. xG and xA are stored in `PlayerStat`. TopSpeed and SprintDistance are stored in `PhysicalMetric`. They are not collapsed into generic attribute ratings.

The CSV fixture remains synthetic development data. It is not live FM26 data and does not prove FM26 stat support.
