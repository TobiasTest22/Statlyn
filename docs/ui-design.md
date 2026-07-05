# UI Design

React/Tauri is the strategic Statlyn desktop UI. Unity is retained as legacy/prototype only. The active React/Tauri direction is a flat financial-analysis-style football recruitment cockpit: black background, charcoal panels, recruitment green active accents, amber caution states, blue/cyan neutral analytics states, cool gray text and dense but readable operational layouts.

The cockpit layout is:

- stable left navigation with official Statlyn branding
- section-aware main workspace for dashboard, recruitment board, data sources and diagnostics
- search/filter foundation over already-safe API rows
- no persistent insight panel; selected player detail belongs in the table workflow or dedicated Player Profile page

The UI should feel like serious internal recruitment intelligence software used by scouts, analysts, sporting directors and recruitment departments. It should be inspired by professional scouting database and financial analytics terminal patterns without copying external player names, logos, numbers or content. The active React/Tauri surface should follow Scout Room-style appearance principles through a flatter analyst lens: narrow sidebar, compact search/filter row, safe model/status cards, large central scout table, selected-row emphasis, diagnostic ledgers, status matrices, thin panel borders and restrained green/amber/blue/red status accents. It should not look like a game UI, crypto dashboard, generic AI dashboard or playful consumer app.

React/Tauri remains display/API-only. It may hold selected-row UI state and render safe DTO values from `Statlyn.Api`; it must not calculate recruitment decisions, read SQLite, call provider code, call the native connector, scrape external data, invent fake rows, expose hidden values or claim FM26 support before validation.

The Diagnostics page may show the safe FM26 snapshot module as a status matrix/risk ledger and audit trail. That module displays only API DTO fields: snapshot status, gate status, blocking reason, next action, latest persisted snapshot and safe snapshot history. It must not imply live player reading, render player rows, expose memory-field details or create fake history. Empty history shows `No persisted snapshots yet`.

The React/Tauri cockpit baseline uses no gradients. Depth comes from black/charcoal layering, thin borders and density rather than decorative effects. Reusable visual modules such as metric cards, status matrices, data-quality bars, confidence bars, distribution strips, diagnostic ledgers, comparison matrices and empty visual states are display-only and must only show safe DTO values. No fake visuals are allowed.

Status colors:

- green: available, passed, safe local data
- amber/yellow: warning, unsupported but expected, insufficient sample
- red: offline, failed, rejected, dangerous state
- blue/cyan: neutral analytics, API, diagnostics, local safe info
- muted gray: no data, not checked, placeholder, no benchmark

Color is never the only meaning. Status text must remain visible. FM26 unsupported must not be green, and no benchmark must not be success.

## Unity Legacy Notes

Statlyn now defaults to a dark command-center desktop interface for Unity. The previous white/glassy look remains as the `LightGlass` legacy fallback theme.

Official logo assets are documented in `docs/branding.md`. The Unity shell uses the official Statlyn logo from `Assets/Resources/Branding`; do not create placeholder logos or pull external branding assets.

The active direction is a premium sports analytics command center: dark navy/charcoal base, teal/cyan active accents, dense readable cards, KPI panels and professional club-operations hierarchy. Milestone 2.7 establishes the theme baseline for the Unity shell and applies it incrementally across the built and placeholder pages.

## Direction

- Dark navy/charcoal background.
- Compact dark panels.
- Subtle borders.
- Teal/cyan active navigation and status accents.
- Rounded controls with an 8px card radius where practical.
- Clear red, amber, green, teal and neutral data states.
- No dark hacker visual language or excessive glow.
- No fake live-data tables.

## First Shell

The current Unity shell creates:

- Left navigation.
- Home dashboard local status overview.
- Scouting firewall and no-live-FM26 status.
- Diagnostics page with Runtime Check and Full Smoke Test.
- Empty and unsupported states for FM26 data.

The first shell intentionally does not show fake live players. Home reads real safe local counts where available and shows `Awaiting local data.` when the runtime database is empty.

Milestone 2.7 adds a reusable command-center helper layer for Unity UI Toolkit: command page headers, status pills, KPI cards, panels, metric rows, action rows, warning banners, data-quality panels, safe empty states and section tabs. These helpers are presentation-only and must not invent values, live FM26 availability, fake benchmarks or external API status.

See `docs/command-center-ui.md` for the compact design-system contract, status categories and page pattern.

The Player Profile v1 page/report loads persisted safe imported data. It does not claim live FM26 connectivity, real player images, club badges or unlicensed flags.

## Player Profile Direction

The player profile should become the design template for later pages. Player Profile v1 includes player identity, source confidence, verdict score cards, role-output evidence, metric groups, data quality, scout actions, reusable visual sections and missing/blocked-data warnings.

Visual copy should stay honest when data is incomplete. Unknown tactical fit should say unknown, low-confidence risk should read as directional rather than precise, missing output should not be drawn as zero, and no percentile or comparison claim should appear unless a real benchmark group exists.

## Persistence And Future Recruitment Surfaces

The first Data Sources screen is wired into the Unity shell for local CSV imports. It should stay functional and honest: manual CSV path entry, source metadata, permission toggles, read-only preview, safe import counts and database diagnostics. It must not show network sources, fake live FM26 data, unlicensed player images, club badges or provider flags.

Recruitment Centre v1 shows persisted imported players as command-center cards with source confidence, completeness, persisted role name, role fit, tactical-fit status, recommendation, risk, output metrics, blocked-field count and missing-data count. Milestone 2.1 adds compact mini visuals for role fit, confidence, completeness, risk, output, missing-data badges and blocked-field badges. It should feel analytical rather than spreadsheet-only, but it remains deliberately simple.

Recruitment Centre screens should show local import status, source permissions, safe audit counts, player stat counts, physical metric counts, reset/default filters and role-output evidence without exposing raw provider snapshots or blocked raw values.

Recruitment UI should not become an attribute-only rating board. Attributes can support evidence, but role-specific performance output, scout observations, sample size, tactical fit and source confidence should be the primary direction.

Player Profile v1 follows this by placing Core Role Output, Supporting Output, Missing Output, Data Quality and Scout Actions before Attribute Support.

## Shortlists Direction

Shortlists v1 closes the first recruitment loop: import CSV, review Recruitment Centre, open Player Profile, add to Shortlist and track a decision. The Shortlists page should stay quiet and operational:

- create shortlist form
- shortlist overview cards
- selected shortlist detail
- player status, priority and follow-up controls
- safe output metrics and warnings
- remove action

Status, priority and follow-up labels are recruitment workflow labels only. They must not imply hidden ability, hidden personality, fee certainty or automatic signing decisions. The UI should keep no-live-FM26 and no-hidden-data copy visible where it matters.

## Scout Desk Direction

Scout Desk v1 extends the loop from shortlist decision tracking into human scouting. It should stay operational, local and qualitative:

- create assignment from persisted player ID or shortlist player
- show assignment cards with player, role, status, priority and latest report
- show generated role/output scout questions
- provide simple qualitative report fields
- show report history and safe notices
- allow optional linked shortlist status update

Scout Desk should not look like a hidden-attribute editor. It must not ask for exact CA, PA, hidden personality or raw blocked values. Mental/character observations are allowed only as qualitative scout notes.

## Role Lab Direction

Role Lab v1 is an operational editor for phase-aware role templates:

- seed generic/import templates
- create simple user roles
- show phase, family, source and official-FM26 status
- show output metric requirements before scout questions and red flags
- create simple IP/OOP role pairs
- keep the copy explicit that FM26 validation is pending

Role Lab should not use old duty language, hidden values or attribute-first scoring. The page is intentionally simple text-field UI for v1; later milestones can deepen editing and visual hierarchy.
## Benchmarks Page

The Benchmarks page is a utility dashboard for generic/import definitions. It includes Statlyn branding, seed and run actions, definition cards, latest run summaries and aggregate snapshot rows.

The page must not show fake benchmark results, fake FM26 verification, hidden values or raw provider data. Empty states should clearly say that no benchmark definitions or no comparison group exists.

## Runtime UI Baseline

Milestone 2.6 keeps the existing white/glassy UI and adds consistency helpers for page headers, safety banners, empty states, error cards and runtime status cards.

Navigation items should never silently fall back to Home. Built pages open directly; unfinished pages show a clear `This page is not built yet` placeholder with no fake data. The Diagnostics page owns Runtime Check and Full Smoke Test controls.

Milestone 2.7 changes the default root class to `theme-dark-command-center` and keeps `LightGlass` documented as a legacy/fallback theme. The shell now shows a global `No live FM26 data` status, a local SQLite runtime note, active navigation state and command-center styling across the Unity pages.

Home now acts as a local command-center overview. It reads safe SQLite counts through `DashboardStatusService` and shows `Awaiting local data.` for an empty database rather than fake player, alert or sync counts. Diagnostics, Runtime Check and Full Smoke Test remain available and unchanged in scope.
