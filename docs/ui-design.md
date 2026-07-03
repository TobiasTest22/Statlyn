# UI Design

Statlyn uses a white, calm, glassy desktop-first interface.

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

The Player Profile slice uses synthetic fixture-mode copy to preview layout only. It does not claim live FM26 connectivity, real player images, club badges or unlicensed flags.

The slice is generated from `MaskedPlayerProfileViewModel` through `UnityProfileRenderModel`, not from raw provider snapshots or hardcoded Unity-only profile data.

## Player Profile Direction

The player profile should become the design template for later pages. It should include player identity, source confidence, scout knowledge, verdict, role evidence, comparison visuals and missing-data warnings.

Visual copy should stay honest when data is incomplete. Unknown tactical fit should say unknown, low-confidence risk should read as directional rather than precise, and percentile comparisons should say fixture comparison group only in fixture mode.

## Persistence And Future Recruitment Surfaces

SQLite persistence is not wired into Unity UI yet. Future Data Sources and Recruitment Centre screens should show local import status, source permissions, safe audit counts, player stat counts, physical metric counts and role-output evidence without exposing raw provider snapshots or blocked raw values.

Recruitment UI should not become an attribute-only rating board. Attributes can support evidence, but role-specific performance output, scout observations, sample size, tactical fit and source confidence should be the primary direction.
