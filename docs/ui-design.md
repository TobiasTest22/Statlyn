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

The first shell intentionally does not show demo players. Fixture data belongs in automated tests only.

The Player Profile slice uses synthetic fixture-mode copy to preview layout only. It does not claim live FM26 connectivity.

The slice is now generated from profile preview data rather than fixed inline labels. The long-term binding target is `MaskedPlayerProfileViewModel`, which is built only from masked data, role scores, source metadata and completeness reports.

## Player Profile Direction

The player profile should become the design template for later pages. It should include player identity, source confidence, scout knowledge, verdict, role evidence, comparison visuals and missing-data warnings.
