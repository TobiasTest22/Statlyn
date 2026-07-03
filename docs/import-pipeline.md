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

The persistence part of an import runs inside one SQLite transaction. Fatal persistence failures roll back player/source/stat/metric rows. Safe row-level failures before persistence can be counted as rejected rows, but raw data is still not stored.

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

Diagnostics may include field names and counts, but not hidden raw values. Diagnostics are sanitized before being written to `ImportAudit`; hidden-value-looking patterns attached to forbidden fields are redacted while safe counts and source names are preserved.

## Re-Import Behavior

Re-importing the same source/player uses snapshot replace for player-scoped data:

- visible fields are replaced
- player stats are replaced
- physical metrics are replaced
- preview role scores are replaced
- blocked audit rows for that player/source entity are replaced
- profile snapshots for that player are replaced

This prevents duplicate rows when the same synthetic CSV fixture is imported twice.

## Performance Output

Imported output metrics are preserved as output data. xG and xA are stored in `PlayerStat`. TopSpeed and SprintDistance are stored in `PhysicalMetric`. They are not collapsed into generic attribute ratings.

The CSV fixture remains synthetic development data. It is not live FM26 data and does not prove FM26 stat support.

Role score recommendations are persisted and reloaded as stored decisions. They are not recomputed during reload.
