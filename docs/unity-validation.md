# Unity Validation

Unity CI is not configured yet. Use this manual checklist after opening `Statlyn.UnityApp` in Unity 6 or newer.

Before opening Unity, run:

```powershell
.\tools\copy-managed-to-unity.ps1
```

This copies the managed Statlyn assemblies, SQLite managed dependencies, Windows x64 SQLite native plugin and the synthetic fixture CSV used by the Player Profile and Data Sources bridge.

## Checklist

- Project opens without package resolution errors.
- `Assets/Scenes/Main.unity` opens.
- No C# compile errors in Unity.
- Main shell loads.
- Navigation is visible.
- Sidebar shows the official Statlyn logo asset from `Assets/Resources/Branding`.
- Dark command-center theme appears with readable text and restrained teal/cyan accents.
- Active navigation state is visible.
- Dashboard header shows a small official Statlyn logo mark.
- Dashboard shows local SQLite overview cards.
- Empty dashboard counts show `Awaiting local data.` rather than fake players, alerts or live sync.
- Dashboard clearly shows `No live FM26 data`.
- Dashboard keeps FM26 memory maps marked unsupported until validated maps exist.
- Player Profile navigation opens the Player Profile v1 page.
- Player Profile page shows persisted-safe/no-live-FM26 header copy and the official Statlyn logo mark.
- `Load First Imported Player` loads a profile after a safe CSV import.
- Manual `StatlynPlayerId` loading works for an imported player.
- Optional Role Lab role/pair text field can be left blank without changing existing profile behavior.
- Optional Role Lab role/pair text field can use a seeded role name after Role Lab seeding.
- Profile report shows identity, source, verdict score cards, role/output fit, core output, supporting output, physical output, data quality/sample size, missing output, warnings, evidence, scout actions, attribute support, blocked-data safe notice and benchmark status.
- Profile report visual components render score cards, horizontal bars, metric tiles, data-quality tiles, evidence cards, warning panels, missing-data panels, blocked-data panels and benchmark status from safe view models only.
- Profile report says generic/import metrics are not FM26-verified.
- Profile report says `No benchmark yet.` and benchmark percentile is unavailable until a real comparison group exists.
- Profile report shows shortlist membership status and `Add to Main Recruitment List`.
- Adding from Player Profile creates or updates the Main Recruitment List safely.
- Profile report shows latest scout report summary or `No scout report yet.`
- Profile report can create a Scout Desk assignment from the loaded persisted player.
- Diagnostics panel loads.
- Data Sources navigation opens a CSV-only page.
- Data Sources page shows the active local SQLite path.
- `Run Runtime Check` reports managed assembly, SQLite managed, SQLite native, database init and workflow service status.
- Runtime check uses a temporary database and does not write to the main `statlyn.db`.
- Manual CSV path entry works.
- `Use synthetic fixture CSV` fills a local fixture path from the repository or StreamingAssets copy.
- `Preview CSV` shows file readability, column count, row count, mapped fields, unknown fields and forbidden fields.
- Preview does not create players, stats or visible fields in SQLite.
- `Run Safe Import` shows accepted/rejected rows and database counts when SQLite dependencies load successfully.
- Safe import shows blocked-field and unknown-field counts.
- Forbidden fields are shown by safe name/category only.
- Recruitment navigation opens the Recruitment Centre page.
- Imported players appear after a safe CSV import.
- Search/source/position/minimum filters refresh the player cards.
- Reset filters returns search/source/position/minimum filters to defaults.
- Sort selector works for role fit, confidence, data completeness, source and position.
- Player cards show source confidence, completeness, persisted role name, role fit score, tactical-fit status, confidence/completeness bars, risk indicator, output mini list, blocked-field badge, missing-data badge and no-live-FM26 label.
- Player cards show `Add to Main Recruitment List`.
- Player cards show a `Shortlisted` badge after the player belongs to an active shortlist.
- `Open Profile` shows the full persisted safe Player Profile report or a safe error card.
- Profile report from Recruitment Centre shows selected player name, source, fixture/import mode, no live FM26 data, role name, role fit/confidence/risk, output metrics, missing-data warning and blocked-data safe notice.
- Shortlists navigation opens the Shortlists page.
- Create shortlist form creates a persisted shortlist.
- Shortlists overview shows shortlist name, player count, active/archive status and updated time.
- Shortlist detail shows persisted players with status, priority, follow-up, role fit, confidence, key output metrics, warnings and no-live-FM26 state.
- Shortlist player rows show latest scout report summary or `No scout report yet.`
- Shortlist player rows can create Scout Desk assignments safely.
- Status, priority, follow-up and user note can be updated safely.
- Remove button removes a player from the shortlist.
- Scout Desk navigation opens the Scout Desk page.
- Scout Desk page shows the official Statlyn logo mark and `Human scouting workflow - qualitative observations only`.
- Scout Desk can create an assignment from a persisted `StatlynPlayerId`.
- Scout Desk can create an assignment for the first shortlisted player.
- Assignment cards show player, role, status, priority, latest report, missing output, blocked audits and no-live-FM26 state.
- Assignment detail shows generated role/output scout questions.
- Scout report form can submit qualitative ratings, recommendation, confidence, strengths, weaknesses, risks, final summary and question answers.
- Linked shortlist update toggle updates shortlist status/follow-up only when selected.
- Report history shows the latest qualitative report.
- Hidden-looking exact text such as `CA 155`, `PA=180` or `Professionalism: 20` is not displayed after report submission.
- Role Lab navigation opens the Role Lab page.
- Role Lab page shows the official Statlyn logo mark and `FM26-style phase roles - not official FM26 mappings yet`.
- Seed roles creates generic/import IP and OOP templates.
- Seeded roles show `Generic/import role template; FM26 validation pending.`
- Create role saves a user-created role.
- Create role pair saves IP/OOP role links and different slots.
- Role detail shows behaviours, metric requirements, scout questions and red flags.
- Role Lab does not show fake official FM26 mappings, old duties, CA/PA or hidden personality values.
- Benchmarks navigation opens the Benchmarks page.
- Benchmarks distinguishes no benchmark, insufficient sample and available benchmark states with text and status color.
- Diagnostics navigation opens the Diagnostics page.
- Runtime Check and Full Smoke Test buttons are still visible.
- Not-built pages use the command-center header and say `This page is not built yet.`
- No fake live FM26 data is shown.
- No real player images, club badges or unlicensed flags are shown.
- Empty/unsupported states remain honest.

If the Unity Editor cannot load SQLite native dependencies, record the runtime-check error, verify `Assets/Plugins/x86_64/e_sqlite3.dll`, rerun `tools/copy-managed-to-unity.ps1`, and keep managed `dotnet test` as the source of truth until Unity packaging is adjusted.

Record the Unity version and screenshots when validating a release candidate.
## Benchmark Validation

Unity validation should include the Benchmarks page after managed assemblies are copied into the Unity project. Check that the page opens, seed definitions works, run definitions works, and snapshot rows show aggregate values only.

Player Profile should still show `No benchmark yet.` without valid definitions or samples. With real imported comparison data, it may show benchmark cards and percentiles only for available results. SQLite-in-Unity must still be manually verified in the Editor before relying on runtime import or benchmark runs there.

See `docs/unity-smoke-test.md` for the automated smoke-test step descriptions and database-mode details.

## Manual Validation Protocol

Opening:

- Open `Statlyn.UnityApp` in Unity 6 or newer.
- Confirm there are no compile errors.
- Confirm the official Statlyn logo appears.
- Confirm all navigation items open either a built page or a safe placeholder.

Runtime:

- Open `Diagnostics`.
- Run `Run Runtime Check`.
- Run `Run Full Smoke Test`.
- Confirm SQLite temp DB initializes.
- Confirm the synthetic fixture is found.
- Confirm no FM26 process is required.

Workflow:

- Open `Data Sources`.
- Select the synthetic CSV fixture.
- Preview the synthetic CSV.
- Run Safe Import.
- Open `Recruitment Centre` and confirm imported synthetic players appear.
- Open `Player Profile` and load the first imported player.
- Add the player to a shortlist.
- Open `Shortlists` and confirm the player appears.
- Create a Scout Desk assignment.
- Submit a qualitative scout report.
- Open `Role Lab` and seed roles.
- Open `Benchmarks`, seed definitions and run definitions.
- Confirm benchmark sections show real status and no fake percentiles.

Safety:

- No raw CurrentAbility, PotentialAbility or Professionalism values are visible.
- No fake live FM26 data is shown.
- No external API or scraping path is used.
- No unlicensed images, badges or provider flags are shown.
- No fake benchmark percentiles are shown.
- Generic/import metrics are clearly labelled as not FM26-verified.

If Unity Editor is not opened, report that clearly. If SQLite-in-Unity is not manually verified, report that clearly.

## Release-Candidate Diagnostics

Milestone 2.8 adds a local product-readiness section to Diagnostics. For a release-candidate pass, run:

- `Run Product Readiness Check`
- `Backup Main Database`
- `Reset Smoke-Test Database`

The readiness check summarizes local SQLite initialization, CSV fixture availability, import workflow construction, Recruitment Centre, Player Profile, shortlists, Scout Desk, Role Lab, Benchmarks and smoke-test service availability. Empty optional areas should be shown as skipped or warning states, not fake success.

The backup action copies only the main local SQLite database file to a timestamped backup under Unity persistent data. The smoke-test reset clears only the temporary smoke-test database path. It must not touch the main runtime database.
