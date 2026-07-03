# Role Lab

Milestone 2.4 adds Role Lab v1, the first editable foundation for phase-aware role modelling.

## Purpose

Role Lab moves Statlyn away from only hardcoded generic role-output profiles and toward persisted, editable role templates.

It supports:

- in-possession roles
- out-of-possession roles
- dual-phase role pairs
- formation slots
- output metric requirements
- scout questions
- red flags
- missing-data confidence rules
- a bridge into Player Profile output summaries

Role Lab is FM26-style, not official FM26 mapping. No seeded role is claimed to be an official FM26 role.

## Safety

Role Lab does not store:

- raw provider snapshots
- raw blocked values
- hidden FM26 values
- CA or PA
- hidden personality values
- old duty fields
- fake official FM26 role mappings

`IsOfficialFm26Role` defaults false. Built-in templates use `Source=BuiltInSeed` and remain generic/import-ready until a future validation milestone marks mappings explicitly.

## Built-In Seed

The seed service creates safe templates such as:

- No-Nonsense Goalkeeper
- Ball-Playing Goalkeeper
- Overlapping Centre-Back
- Playmaking Wing-Back
- Advanced Wing-Back
- Inside Wing-Back
- Midfield Playmaker
- Wide Central Midfielder
- Wide Forward
- Channel Forward
- High Press Role
- Mid Block Role
- Low Block Role
- Wide Defensive Role
- Central Screening Role
- Recovery Cover Role
- Aggressive Pressing Role
- Passive Containment Role
- Back Line Holding Role
- Pressing Defensive Midfielder

These are generic/import templates. They are not a complete FM26 role list and not old FM24 duty templates.

## Output Profile Bridge

`RoleLabOutputProfileBridge` converts a tactical role or role pair into the existing `RoleOutputExpectationProfile` shape. Player Profile can use a selected Role Lab role or pair name through its optional profile selector.

Fallback order:

1. selected Role Lab role or pair
2. persisted role-output profile
3. generic/import seed profile

## Unity Page

The Role Lab page includes:

- official Statlyn branding
- seed roles button
- simple create-role form
- role list with phase, family, source and FM26 status
- role detail with behaviours, metrics, scout questions and red flags
- simple role-pair form

V1 is intentionally simple and text-field based. A later milestone can add richer editing and selection without changing the safe persistence boundary.
