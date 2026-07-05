# Advanced Recruitment Analysis Roadmap

This roadmap describes future methods for Statlyn player intelligence. Each method must be implemented as backend C# analytics behind Statlyn.Api. React/Tauri remains display/API-only and does not calculate recruitment decisions, prices, role scores, fit scores or benchmark outputs.

No fake data is allowed. FM26 remains unsupported for player reading until validated maps, reviewed C++ read-only connector work and safe C# snapshot services exist.

## Methods

### Player Style Vector

Required data:
- common safe player metrics
- minutes
- position or role
- source confidence

Safe output:
- vector key
- metric values
- data quality
- confidence

Unavailable state:
`Archetype unavailable. Safe style vector metrics are missing.`

Not allowed:
- CA/PA
- hidden personality
- hidden consistency
- raw memory values

Future FM26 dependency:
- only if visible FM26-exported or validated snapshot metrics are proven safe.

### Role Fit Model

Required data:
- role parameter definition
- primary and secondary role metrics
- minutes threshold
- safe role evidence

Safe output:
- role assessment
- missing role metrics
- confidence
- warnings

Unavailable state:
`Role assessment unavailable. Missing role-specific safe metrics.`

Not allowed:
- hidden ability values
- frontend scoring
- unsupported FM26 fields

### Squad Need Model

Required data:
- squad depth
- tactical role needs
- team style model
- current player availability if safely imported

Safe output:
- squad need label
- urgency band
- missing squad inputs
- confidence

Unavailable state:
`Squad need unavailable. Team squad model has not been defined.`

Not allowed:
- live FM squad reads before connector validation
- invented squad gaps

### Tactical Fit Projection

Required data:
- team style model
- player style vector
- tactical role or role pair
- squad need

Safe output:
- fit projection summary
- confidence
- missing fields

Unavailable state:
`Fit projection unavailable. Team style model or squad need has not been defined.`

Not allowed:
- invented chemistry
- hidden personality
- raw FM tactical data

### Expected Impact

Required data:
- per-90 outputs
- league average comparison
- role fit
- minutes weighting
- squad need

Safe output:
- expected impact band
- confidence
- key drivers and discounts

Unavailable state:
`Expected impact unavailable. Missing role outputs, league sample or squad need.`

Not allowed:
- fake projections
- hidden values

### Similar Player Search

Required data:
- comparable safe player sample
- common metric set
- position or role
- minutes threshold

Safe output:
- candidate list
- similarity score
- data quality
- confidence

Unavailable state:
`Similar player search unavailable. Not enough comparable safe player data.`

Not allowed:
- fake alternatives
- scraped provider rows
- hidden attributes

### Replacement Shortlist

Required data:
- current squad role needs
- comparable player sample
- transfer context if safe
- shortlist workflow status

Safe output:
- replacement candidate list
- reason text
- missing data
- confidence

Unavailable state:
`Replacement shortlist unavailable. Squad need or comparable sample is missing.`

Not allowed:
- invented targets
- unlicensed provider data

### League-Adjusted Performance

Required data:
- player per-90 metrics
- league/competition strength if safely imported
- league sample

Safe output:
- adjusted metric values
- confidence
- league-strength explanation

Unavailable state:
`League adjustment unavailable. League strength data has not been imported.`

Not allowed:
- fake league strength
- unsupported FM assumptions

### Age Curve / Development Trajectory

Required data:
- age
- role
- performance sample
- comparable historical sample if available

Safe output:
- age-curve band
- uncertainty
- confidence

Unavailable state:
`Development trajectory unavailable. Missing age, role or historical sample.`

Not allowed:
- PA
- hidden professionalism
- hidden ambition
- hidden development traits

### Value vs Performance

Required data:
- Statlyn Fair Value Estimate
- per-90 performance
- benchmark comparison
- minutes/sample size

Safe output:
- value band
- over/under-value signal
- confidence
- missing inputs

Unavailable state:
`Value vs performance unavailable. Missing fair value or benchmark performance.`

Not allowed:
- fake price
- fake benchmark percentile
- hidden reputation

### Uncertainty And Data Quality

Required data:
- data completeness
- source confidence
- missing fields
- sample size

Safe output:
- confidence score
- data quality label
- warnings

Unavailable state:
`Uncertainty model unavailable. Source confidence or sample metadata is missing.`

Not allowed:
- hidden confidence inputs
- frontend scoring

### Minutes / Sample-Size Weighting

Required data:
- minutes
- match count if available
- competition context if available

Safe output:
- sample strength
- confidence adjustment
- range widening

Unavailable state:
`Sample weighting unavailable. Minutes or match sample is missing.`

Not allowed:
- pretending small samples are high confidence

### Role-Specific Benchmark Groups

Required data:
- benchmark definition
- player sample
- role or position
- metric set

Safe output:
- aggregate benchmark comparison
- sample size
- missing metric list

Unavailable state:
`Role benchmark unavailable. Not enough safe comparison players.`

Not allowed:
- fake percentiles
- individual hidden benchmark values

### Team Style Compatibility

Required data:
- team style profile
- player style vector
- tactical role parameters

Safe output:
- compatibility band
- drivers
- missing fields

Unavailable state:
`Team style compatibility unavailable. Team style model has not been defined.`

Not allowed:
- invented style model
- hidden personality

### Goalkeeper-Specific Model

Required data:
- save percentage
- shots faced
- goals prevented if available
- xG against if available
- claims and handling indicators if available
- distribution metrics

Safe output:
- shot-stopping assessment
- sweeper keeper assessment
- confidence and missing metrics

Unavailable state:
`Goalkeeper model unavailable. Missing goalkeeper-specific safe metrics.`

Not allowed:
- hidden GK ability
- unsupported FM hidden attributes

### Centre-Back Ball Progression Model

Required data:
- progressive carries
- progressive passes
- pass completion
- defensive duel success
- aerial success
- risk/error indicators if safe

Safe output:
- progression profile
- defensive security summary
- confidence

Unavailable state:
`Centre-back progression model unavailable. Missing progression or defensive metrics.`

Not allowed:
- hidden consistency
- raw memory values

### Winger Take-On Model

Required data:
- dribbles attempted
- dribble success
- carries into final third
- progressive carries
- touches in box
- chances created
- turnovers/risk

Safe output:
- take-on threat profile
- risk profile
- confidence

Unavailable state:
`Take-on model unavailable. Missing dribble and carry metrics.`

Not allowed:
- fake dribble metrics
- frontend calculation

### Controller Midfielder Model

Required data:
- pass volume
- progressive passes
- pass completion
- key passes
- press resistance if available
- touches in middle/final third
- tempo involvement

Safe output:
- control profile
- progression profile
- confidence

Unavailable state:
`Controller model unavailable. Missing passing, tempo or pressure metrics.`

Not allowed:
- hidden composure
- hidden pressure
- unsupported FM mental values

## Implementation Rule

Every future method must define:
- required data
- optional data
- minimum sample size
- confidence and data-quality behavior
- safe unavailable state
- forbidden input list
- API DTO shape
- React display-only module state

The system should be capable of ingesting future FM data through C++ and real-life data through permitted imports/providers, but each source must pass the same safe-field policy before analytics can use it.
