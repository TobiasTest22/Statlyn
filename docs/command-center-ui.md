# Command-Center UI

Milestone 2.7 moves Statlyn's Unity shell toward a dark football recruitment command center while keeping the existing safe local workflow intact.

## Theme

- `DarkCommandCenter` is the primary Unity baseline.
- `LightGlass` remains a legacy/fallback theme.
- Official Statlyn logo assets from `Assets/Resources/Branding` are the only logo assets used.
- The dark theme uses navy/charcoal backgrounds, compact panels, subtle borders and restrained teal/cyan accents.

## Status System

Statuses must always have text. Color is supporting context only.

- `Success`: passed checks, available benchmark state, safe local workflow ready.
- `Info`: CSV-only, no live FM26 data, generic/import metrics, local database context.
- `Warning`: unsupported FM26, insufficient sample, missing metric, blocked/guardrail state.
- `Danger`: runtime check failed, smoke test failed, safe runtime error.
- `Muted`: no benchmark, not built yet, not run, awaiting local data.
- `Accent`: selected or active UI emphasis.

Unsupported FM26 must never render as a healthy/live green state. No benchmark must never render as an available benchmark.

## Page Pattern

Built pages should prefer:

- command-center header
- short page subtitle
- safety/status banner
- primary action row
- compact KPI/status cards
- command panels for row-heavy details
- textual empty and error states

Pages must keep existing workflow controls visible. Diagnostics owns Runtime Check and Full Smoke Test. Data Sources owns CSV preview/import.

## Safety

The UI must not invent data to look busy. It must not show fake live FM26 data, fake KPI numbers, fake alerts, fake benchmark percentiles, unlicensed images, club badges, provider flags, raw provider snapshots, hidden values or raw blocked values.

Home reads local SQLite counts through `DashboardStatusService`. Empty databases show `Awaiting local data.` rather than placeholder recruitment numbers.

## Validation

Managed tests verify theme tokens, status mapping, safe navigation metadata, dashboard overview counts and smoke-test compatibility. Unity Editor validation remains manual unless a release note says it was opened and checked locally. SQLite-in-Unity must be validated through the Diagnostics page runtime check before runtime import is trusted in the Editor.
