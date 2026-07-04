# React/Tauri UI

`Statlyn.Desktop` is the strategic desktop UI. It is a React + Tauri premium football recruitment analyst cockpit with a black glass visual direction, dense readable tables, safe status cards, loading states, empty states, error states and an API status indicator.

The target feeling is serious internal recruitment intelligence software: dark, clean, glassy, data-heavy and professional. It should feel like football analytics mixed with a finance-terminal interface, not a generic AI dashboard.

The desktop UI uses official Statlyn logo assets from `Statlyn.Desktop/public/branding/`. Dark glass surfaces should use the white wordmark and white mark variants; black-text variants are reserved for future light export, print or onboarding surfaces.

The desktop UI may handle:

- navigation
- layout
- display DTOs
- API calls
- filtering and sorting over already-safe DTOs
- local UI state
- connector status display from `/connector/status`
- selected-row UI state for inspecting already-safe API rows

The desktop UI must not contain:

- recruitment scoring
- role scoring
- benchmark calculations
- scouting firewall rules
- hidden-value filtering
- provider mapping
- FM memory logic
- native connector calls
- direct database access

## Cockpit Layout

The React/Tauri shell follows a three-part analyst cockpit layout:

- left navigation with official Statlyn branding, stable page links and clear active state
- main workspace with page title, operational status cards, safe tables/lists and API-down or empty states
- right insight rail for selected target context, recruitment status summary and no-live-FM26 reminders

The selected-row workflow is scan, inspect, decide. The recruitment board can highlight a selected safe API row and mirror that row into the insight rail. If no row exists, the rail shows a safe empty state. It must not invent players, profile details, charts, alerts or KPIs.

## Visual System

- background: black with subtle dark analyst depth
- surfaces: slightly lighter glass panels
- primary accent: recruitment green
- secondary accents: amber/yellow for caution and blue/cyan for neutral analytics
- text: white and cool gray
- borders: subtle and soft
- glow: restrained and only for selected/active states
- density: expert-level but readable

Color is supporting context only. Every status also needs text. FM26 unsupported must never render as a green/success state, and no benchmark must not render as success.

## Development Run

```powershell
cd Statlyn.Desktop
npm install
npm run dev
```

Start `Statlyn.Api` separately before expecting connected data:

```powershell
dotnet run --project ..\Statlyn.Api\Statlyn.Api.csproj --urls http://localhost:5118
```

The default API URL is `http://localhost:5118`. Override it with `VITE_STATLYN_API_URL` if needed. API must be started separately in the current development mode; the Tauri app does not yet launch the API for you.

## Checks And Packaging

Use the desktop scripts from `Statlyn.Desktop`:

```powershell
npm run typecheck
npm run check
npm run build
npm run tauri:dev
npm run tauri:build
```

`npm run check` runs TypeScript checking and the Vite production build. `npm run tauri:build` produces installer bundles under:

```text
Statlyn.Desktop/src-tauri/target/release/bundle/msi/
Statlyn.Desktop/src-tauri/target/release/bundle/nsis/
```

The root validation helper can run the local stack checks:

```powershell
.\tools\run-desktop-validation.ps1
```

It does not require FM26 or Unity and stops its temporary API process before exiting.

## Safe States

The desktop UI must show honest states for:

- API unavailable
- API error
- empty database
- no imported players
- no selected profile
- FM26 unsupported
- benchmark unavailable
- no scout reports
- no Role Lab templates
- no data sources

No fake data is a hard rule. The UI must not create fake rows, fake KPIs, fake sync status, fake live FM26 data or hidden-value displays. If the API cannot be reached, the UI shows a safe connection error and retry button.

The Connector Status panel is informational only. It displays the API-reported binding availability, FM process detection state, read-only access status and support message. A detected FM process must still render as unsupported until validated maps exist.

## API Bundling Decision

Future packaging options:

1. Tauri launches `Statlyn.Api` as a sidecar process.
2. `Statlyn.Api` runs as a separate local service.
3. Tauri uses a C# library bridge later.
4. Keep current development mode, where the API is started separately.

Recommendation for now: keep the API separate for development validation. Sidecar bundling should be a later milestone after the API contract and desktop validation path stay stable.

## Current Limitations

- FM26 live memory is unsupported until validated maps and connector milestones exist.
- No real player data appears unless safe local data is imported into SQLite.
- NPM audit findings are documented in `docs/npm-audit-notes.md`; do not run `npm audit fix --force`.
