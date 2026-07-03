# UI Design

Statlyn uses a white, calm, glassy desktop-first interface.

Official logo assets are documented in `docs/branding.md`. The Unity shell uses the official Statlyn logo from `Assets/Resources/Branding`; do not create placeholder logos or pull external branding assets.

## Direction

- Off-white background.
- Frosted white panels.
- Subtle borders.
- Soft shadows.
- Rounded controls with an 8px card radius where practical.
- Clear red, orange, green, blue and grey data states.
- No dark hacker visual language.
- No fake live-data tables.

## First Shell

The current Unity shell creates:

- Left navigation.
- Home dashboard state panels.
- Scouting firewall status.
- Advanced diagnostics panel.
- Empty and unsupported states for FM26 data.
- First Player Profile slice in clearly marked fixture mode.

The first shell intentionally does not show fake live players. The Player Profile slice may show one synthetic development fixture, clearly labelled as fixture mode.

The Player Profile slice uses synthetic fixture-mode copy to preview dashboard layout only. The Player Profile v1 page/report loads persisted safe imported data. Neither surface claims live FM26 connectivity, real player images, club badges or unlicensed flags.

The slice is generated from `MaskedPlayerProfileViewModel` through `UnityProfileRenderModel`, not from raw provider snapshots or hardcoded Unity-only profile data.

## Player Profile Direction

The player profile should become the design template for later pages. Player Profile v1 includes player identity, source confidence, verdict, role-output evidence, data quality, scout actions, simple visual sections and missing/blocked-data warnings.

Visual copy should stay honest when data is incomplete. Unknown tactical fit should say unknown, low-confidence risk should read as directional rather than precise, and no percentile or comparison claim should appear unless a real benchmark group exists.

## Persistence And Future Recruitment Surfaces

The first Data Sources screen is wired into the Unity shell for local CSV imports. It should stay functional and honest: manual CSV path entry, source metadata, permission toggles, read-only preview, safe import counts and database diagnostics. It must not show network sources, fake live FM26 data, unlicensed player images, club badges or provider flags.

Recruitment Centre v1 shows persisted imported players as white/glassy cards with source confidence, completeness, persisted role name, role fit, tactical-fit status, recommendation, risk, output metrics, blocked-field count and missing-data count. It should feel analytical rather than spreadsheet-only, but it remains deliberately simple.

Recruitment Centre screens should show local import status, source permissions, safe audit counts, player stat counts, physical metric counts, reset/default filters and role-output evidence without exposing raw provider snapshots or blocked raw values.

Recruitment UI should not become an attribute-only rating board. Attributes can support evidence, but role-specific performance output, scout observations, sample size, tactical fit and source confidence should be the primary direction.

Player Profile v1 follows this by placing Core Role Output, Supporting Output, Missing Output, Data Quality and Scout Actions before Attribute Support.
