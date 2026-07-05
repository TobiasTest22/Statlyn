# Player Intelligence Data Availability Matrix

This matrix defines when Player Intelligence features may operate. Statlyn must not assume FM26, CSV, SQLite, real-life providers or future connectors provide a field until the field is actually present in a safe import or validated snapshot contract.

Classifications:
- Available now from safe local CSV/import
- Available from existing Statlyn SQLite
- Available later from validated FM26 snapshot/map
- Requires event data import
- Requires external licensed/permitted provider
- Not available
- Unknown, must be verified

## Matrix

| Feature | Current classification | Required input fields | Optional input fields | Minimum sample | Quality requirements | Unavailable state | Safety restrictions |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Player Profile | Available from existing Statlyn SQLite when players exist | StatlynPlayerId, DisplayName, SourceName | Age, Nationality, PrimaryPosition, role score, warnings | 1 player | Masked safe player row | `Player profile was not found in persisted safe data.` | No hidden values, no CA/PA, no raw provider data |
| Skill Radar / Spider Profile | Available now from safe local CSV/import if enough numeric safe metrics exist | playerId, metric name, metric value, confidence | role, benchmark axis | 3 axes | Display/scoring allowed by field policy | `Skill radar unavailable. Missing enough safe visible or imported metrics.` | No hidden attributes, no CA/PA, no generated axes |
| Match Heatmaps | Requires event data import | matchId, playerId, eventType, x, y, minute | teamId, opponent, action subtype | At least 1 imported point, stronger at 200+ points | Coordinates must be safe, imported and source-tagged | `Heatmap unavailable. No safe event-location data has been imported.` | Do not fake heatmaps or infer locations from summaries |
| Performance Per 90 | Available now from safe local CSV/import when totals and minutes exist | playerId, metric name, metric value, minutes | unit, role, competition | 900 minutes recommended | Minutes must not be missing; calculation is C# server-side | `Performance per 90 unavailable. Missing minutes or safe performance metrics.` | No frontend calculation, no fake totals |
| Fair Price / Value Estimate | Available when safe valuation anchor or comparable sample exists | valuation anchor or safe comparable prices, age, minutes, role or position | contract end, wage, league strength, tactical fit, risk flags | 900 minutes recommended; 5 comparable players if no anchor | Must show range and confidence, not truth | `Fair value unavailable. Missing valuation anchor, contract context or comparable player sample.` | No CA/PA, no hidden reputation, no scraped prices, no fake comparables |
| Expected Fit If Joined Team | Requires team style and squad need definition | player style vector, role model, team style model, squad need | tactical fit, benchmark context | Role sample threshold from role definition | Team style must be imported/defined | `Fit projection unavailable. Team style model or squad need has not been defined.` | No invented chemistry or personality |
| Player Archetypes | Requires style vector or role metric set | playerId, role, common style metrics, minutes | benchmark group | 900 minutes recommended and 4 metrics | Safe vectors with confidence | `Archetype unavailable. Safe style vector metrics are missing.` | No generated archetypes without evidence |
| Similar Players | Requires comparable safe player sample | comparable sample, common metric set, position/role, minutes threshold | comparable price if permitted | 5 comparable players minimum | Same metric keys across players | `Similar player search unavailable. Not enough comparable safe player data.` | No fake alternatives, no hidden values |
| League Average Comparison | Requires league sample | leagueKey, position/role, metric name, average value, sample size | percentile, min/max if aggregate-only | 10 players minimum, larger preferred | Show sample size and insufficient-data states | `League comparison unavailable. Not enough league sample data.` | No fake percentiles, no individual hidden values |
| Playstyle Fit | Requires team style model | player style vector, team style model, role-specific parameters | squad need, tactical role pair | Role threshold from definition | Backend C# comparison only | `Fit projection unavailable. Team style model or squad need has not been defined.` | React displays DTOs only |

## Optional Safe Tables

Milestone 4.0 adds optional schema slots for future operational data:

- `player_match_performance`
- `player_event_locations`
- `player_market_context`
- `team_style_profiles`
- `league_average_metrics`
- `player_style_vectors`

These tables are optional. Existing imports continue to work. Empty tables must produce unavailable states, not generated data.

## Feature Requirements

### Heatmaps

Required:
- matchId
- playerId
- eventType
- x
- y
- minute

Optional:
- teamId
- opponent

Minimum sample:
- 1 point for a basic view
- 200+ points for stronger confidence

Unavailable:
`Heatmap unavailable. No safe event-location data has been imported.`

Safety:
- no fake heatmap points
- no coordinate guessing
- no external scraping

### Fair Price / Value Estimate

Required:
- current value, expected price, asking price, value range or safe comparable player price sample
- age
- position or role
- minutes/performance sample

Optional:
- contract end or contract status
- wage
- league/competition level
- league strength
- benchmark performance
- tactical fit
- squad need
- visible/imported risk indicators

Minimum sample:
- 900 minutes recommended for performance adjustment
- 5 safe comparable players if no direct anchor exists

Unavailable:
`Fair value unavailable. Missing valuation anchor, contract context or comparable player sample.`

Safety:
- internal estimate only
- show a range and confidence
- no fake price
- no hidden reputation, CA, PA, hidden personality, hidden injury proneness or scraped data

### Similar Players

Required:
- comparable sample
- common metric set
- position/role
- minutes threshold

Optional:
- safe comparable prices
- league adjustment
- age band

Minimum sample:
- 5 comparable players

Unavailable:
`Similar player search unavailable. Not enough comparable safe player data.`

Safety:
- no fake alternatives
- no hidden values
- confidence must drop when data completeness is weak

### League Average Comparison

Required:
- leagueKey
- metricName
- averageValue
- sampleSize
- positionGroup or roleName

Optional:
- minutes threshold
- min/max aggregate
- source confidence

Minimum sample:
- 10 player sample minimum

Unavailable:
`League comparison unavailable. Not enough league sample data.`

Safety:
- show sample size
- insufficient data must not appear as success
- no fake percentiles

### Playstyle Fit

Required:
- player style vector
- team style model
- role parameters
- squad need or tactical role context

Optional:
- role pair
- phase model
- benchmark group

Unavailable:
`Fit projection unavailable. Team style model or squad need has not been defined.`

Safety:
- no invented chemistry
- no hidden personality
- backend C# only

## FM26 Reality

FM26 remains unsupported for player reading. The C++ connector must stay read-only and connector-only. Validated FM26 snapshot/map support may later populate safe fields, but Milestone 4.0 does not start player memory reading and does not assume FM26 provides event coordinates, heatmaps, advanced stats, market values or per-90 data.

If future FM26 output does not expose a field safely, Statlyn must keep the module unavailable.
