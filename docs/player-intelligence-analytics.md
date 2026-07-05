# Player Intelligence Analytics Foundation

Milestone 4.0 defines the safe foundation for advanced player recruitment and performance analysis. The goal is operational readiness, not invented output. Statlyn may show player profiles, radar profiles, heatmaps, per-90 analysis, fair value, similar players, archetypes, playstyle fit and league comparison only when safe imported or validated data exists.

React/Tauri is the strategic desktop UI. React/Tauri remains display/API-only. Statlyn.Api is the local boundary, C# remains the analytics and decision layer, SQLite is local storage, and the native connector remains connector-only. FM26 remains unsupported for player reading until validated maps and reviewed snapshot readers exist.

No fake data is allowed. No hidden values, CA, PA, raw provider snapshots, memory addresses, offsets or raw blocked values may appear in models, API DTOs or UI.

## Feature Map

### 1. Player Profile

Purpose: identity and recruitment context for a selected safe player.

Inputs:
- identity and Statlyn player id
- position and role
- source and source confidence
- age and nationality
- safe data quality
- backend role fit and confidence
- warnings and missing data

Restrictions:
- no hidden FM values
- no CA or PA
- no invented personality, chemistry or reputation

Unavailable state:
`Player profile was not found in persisted safe data.`

### 2. Skill Radar / Spider Profile

Purpose: technical, physical, mental/tactical and role-specific profile axes from safe visible/imported data.

Inputs:
- safe visible numeric attributes where permitted
- safe player stats or physical metrics
- role-specific metric definitions
- benchmark values only when a real benchmark exists

Restrictions:
- no CA or PA
- no hidden attributes
- no generated axes

Unavailable state:
`Skill radar unavailable. Missing enough safe visible or imported metrics.`

### 3. Match Heatmaps

Purpose: visual location summary for recent match actions.

Required event fields:
- matchId
- playerId
- eventType
- x
- y
- minute

Optional fields:
- teamId
- opponent
- action subtype

Restrictions:
- heatmaps require imported event-location data
- do not fake coordinates
- do not infer locations from non-location stats

Unavailable state:
`Heatmap unavailable. No safe event-location data has been imported.`

### 4. Performance Per 90

Purpose: server-side per-90 calculation from safe totals and minutes.

Possible metrics:
- minutes
- goals and xG if available
- assists and xA if available
- shots
- key passes
- carries
- progressive passes
- defensive actions
- aerials
- goalkeeper metrics
- role-specific metrics

Rules:
- all calculations are C# server-side
- missing minutes means unavailable
- weak sample lowers confidence

Unavailable state:
`Performance per 90 unavailable. Missing minutes or safe performance metrics.`

### 5. Fair Price / Value Estimate

Purpose: transparent internal estimate, not an official truth.

Label:
`Statlyn Fair Value Estimate`

Allowed inputs:
- current value, expected price, asking price or valuation range if safely imported
- age
- contract length or contract end date if available
- wage if available
- visible/imported reputation only, never hidden reputation
- league level or competition strength if available
- position and role
- minutes played
- role-specific performance per 90
- benchmark vs league, position or role average
- data completeness
- visible/imported injury or risk indicators only
- squad need or tactical fit if defined in Statlyn
- similar player prices only from safe comparable data

Forbidden inputs:
- CA
- PA
- hidden reputation values
- hidden personality
- hidden consistency
- hidden injury proneness
- raw memory values
- raw provider snapshots
- scraped market data
- fake comparable prices
- fake contract or wage data

Model:

```text
FairValue = AnchorValue
            * PerformanceMultiplier
            * AgeCurveMultiplier
            * ContractMultiplier
            * RoleScarcityMultiplier
            * LeagueStrengthMultiplier
            * TacticalFitMultiplier
            * RiskAdjustment
            * DataConfidenceAdjustment
```

Output:
- available
- fairValueLow
- fairValueMid
- fairValueHigh
- currency
- valueIndex if currency estimate is unavailable
- confidence
- dataQuality
- keyValueDrivers
- keyDiscountDrivers
- missingInputs
- modelVersion
- safeMessage

If no safe anchor or sufficient comparable sample exists, Statlyn returns unavailable instead of inventing a price.

Unavailable state:
`Fair value unavailable. Missing valuation anchor, contract context or comparable player sample.`

### 6. Expected Fit If Joined Team

Purpose: projection against our team style, squad need and role model.

Inputs:
- role model
- tactical fit
- squad need
- style profile
- data quality

Restrictions:
- no hidden values
- no invented chemistry
- unavailable when team style or squad need is missing

Unavailable state:
`Fit projection unavailable. Team style model or squad need has not been defined.`

### 7. Player Archetypes

Supported archetype targets:
- take-on winger
- direct runner
- game controller
- defence breaker
- ball-carrying centre-back
- defensive stopper
- pressing forward
- creative 10
- ball-winning midfielder
- shot-stopping goalkeeper
- sweeper keeper
- aerial dominant CB
- progression full-back

Rules:
- archetypes require safe role/style metrics
- confidence depends on data completeness and minutes
- no generated archetype if vectors are missing

Unavailable state:
`Archetype unavailable. Safe style vector metrics are missing.`

### 8. Similar Players

Purpose: find alternatives using safe statistical/style vectors.

Inputs:
- comparable sample
- common metric set
- position or role
- minutes threshold

Rules:
- no hidden values
- confidence depends on completeness and common fields
- no fake alternatives

Unavailable state:
`Similar player search unavailable. Not enough comparable safe player data.`

### 9. League Average Comparison

Purpose: compare a player to league, position or role averages.

Inputs:
- league sample
- common metric set
- sample size
- minutes threshold

Rules:
- show sample size
- insufficient sample stays unavailable
- no fake percentiles

Unavailable state:
`League comparison unavailable. Not enough league sample data.`

### 10. Playstyle Fit

Purpose: compare player style to our defined team style model.

Inputs:
- player style vector
- team style model
- role-specific parameters
- squad need

Rules:
- team style must be imported or defined
- role-specific parameters are required
- missing team style returns unavailable

Unavailable state:
`Fit projection unavailable. Team style model or squad need has not been defined.`

## Backend Contracts

Milestone 4.0 adds safe model contracts under `Statlyn.Analytics.PlayerIntelligence`:

- `PlayerIntelligenceProfile`
- `PlayerSkillRadar`
- `PlayerRadarAxis`
- `PlayerPer90Metric`
- `PlayerPer90Summary`
- `PlayerHeatmapSummary`
- `PlayerHeatmapPoint`
- `PlayerValueEstimate`
- `PlayerFitProjection`
- `PlayerArchetypeResult`
- `PlayerSimilarityResult`
- `SimilarPlayerCandidate`
- `LeagueAverageComparison`
- `RoleSpecificAssessment`
- `RoleParameterDefinition`
- `RoleParameterMetric`
- `PlayerStyleVector`
- `TeamStyleModel`
- `PlayerDataAvailabilityReport`

Every result carries availability, safe message, confidence, data quality, missing fields and warnings where relevant.

## API Endpoints

Safe API endpoints:

- `GET /analytics/player-intelligence/readiness`
- `GET /players/{id}/intelligence`
- `GET /players/{id}/radar`
- `GET /players/{id}/per90`
- `GET /players/{id}/heatmap`
- `GET /players/{id}/similar`
- `GET /players/{id}/fit`
- `GET /players/{id}/value`
- `GET /players/{id}/league-comparison`

Endpoints return unavailable DTOs when data is missing. They must not return fake heatmap points, fake similar players or fake fair value ranges.
