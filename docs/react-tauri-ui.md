# React/Tauri UI

`Statlyn.Desktop` is the strategic desktop UI. It is a React + Tauri professional dark football recruitment analyst cockpit with a flat financial-analysis-style visual direction, dense readable tables, safe status cards, loading states, empty states, error states and an API status indicator.

The current visual target is a flat financial analyst recruitment terminal in Scout Room-style appearance principles only: narrow left rail, compact top search/filter row, dense central scout table, flat charcoal panels, model/status visual modules, restrained recruitment green accents and subtle amber/blue/red status treatment. It must not copy external player names, logos, club marks, fake ratings, fake charts or proprietary workflows.

The target feeling is serious internal recruitment intelligence software: dark, clean, compact, data-heavy and professional. It should feel like football analytics mixed with a financial analytics terminal, not a game UI, fantasy app or generic AI dashboard.

The desktop UI uses official Statlyn logo assets from `Statlyn.Desktop/public/branding/`. Dark glass surfaces should use the white wordmark and white mark variants; black-text variants are reserved for future light export, print or onboarding surfaces.

The desktop UI may handle:

- navigation
- layout
- display DTOs
- API calls
- filtering and sorting over already-safe DTOs
- local UI state
- connector status display from `/connector/status`
- selected-row UI state for highlighting already-safe API rows

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

The React/Tauri shell follows a focused scout-room cockpit layout:

- left navigation with official Statlyn branding, stable page links and clear active state
- main workspace with page title, search/filter foundation, safe model/status cards, a large table-first recruitment board and API-down or empty states
- no persistent insight panel; selected detail belongs in the table workflow or the dedicated Player Profile page

The selected-row workflow is scan, select and continue through safe Statlyn pages. The recruitment board can highlight a selected safe API row, but it must not create a persistent right-side insight panel, invent players, profile details, charts, alerts or KPIs.

## Visual System

- background: black with subtle dark analyst depth
- surfaces: flat charcoal panels using thin borders and low decoration
- primary accent: recruitment green
- secondary accents: amber/yellow for caution and blue/cyan for neutral analytics
- text: white and cool gray
- borders: subtle and soft
- glow/effects: minimal; selected and active states rely on text, borders and a restrained green indicator
- density: expert-level but readable
- no gradients; the interface should feel analytical, not game-like
- no fake visuals; bars, matrices, ledgers and placeholders may only represent safe API DTO values

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

For Milestone 3.3 and 3.3.5, the FM26 Diagnostics surface displays safe API DTO fields only: native connector availability, Windows/platform state, FM process detected/not detected, safe executable file/folder labels, version/build metadata where available, read-only access, memory-map registry counts, selected-map safe summary, map status and next action. FM process detection and map selection are diagnostics only. Memory maps remain metadata-only templates until validated. No player data is read. FM26 remains unsupported for player reading. React/Tauri never calls native connector directly, never parses map files, never inspects local processes and never reads SQLite directly.

First safe player snapshot is later.

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
