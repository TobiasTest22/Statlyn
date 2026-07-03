# Role Output Expectations

Milestone 1.7 adds generic role-output expectation profiles for future role-specific scoring. Milestone 2.4 adds Role Lab as the first editable phase-aware source of role-output expectations.

These are not final FM26 roles, not FM24 duty templates and not FM26-specific assumptions. They are neutral templates that keep different position groups from being judged by the same outputs.

## Seeded Generic Profiles

- Generic Goalkeeper Output
- Generic Centre-Back Output
- Generic Wide Attacker Output
- Generic Striker Output
- Generic Central Midfielder Output

## Direction

Goalkeepers should use goalkeeper output such as saves, save percentage, goals prevented and distribution.

Centre-backs should use defensive and build-up output such as aerial success, clearances, blocks, interceptions and progressive passing.

Wide attackers can use xA, dribbles, progressive carries, key passes, crosses and shot threat.

Strikers can use xG, shots, goals and optional pressing output where the role demands it.

Midfielders should vary based on build-up, ball-winning, carrying, creativity and defensive output.

Missing performance data lowers confidence. It must not be filled with fake zero values.

Attributes are supporting evidence, not the majority of a role-output profile.

Recruitment Centre v1 uses these profiles for output-first row summaries:

- wide attackers prefer creative/carrying/threat output such as xA, progressive carries and xG when available
- centre-backs prefer defensive/aerial/build-up output
- goalkeepers prefer goalkeeper output and are not judged by winger metrics
- strikers prefer xG, shots and goals
- central midfielders prefer progression, chance creation and defensive contribution

Missing core metrics are displayed as missing warnings, not zeroes.

Milestone 1.9.1 changes the Recruitment Centre query path to prefer persisted `RoleOutputExpectationProfile` rows from SQLite. Milestone 2.0 applies the same selection path to Player Profile v1. Milestone 2.4 adds `RoleLabOutputProfileBridge`, which can convert selected `TacticalRole` or `TacticalRolePair` metric requirements into the same profile shape.

Selection order:

- selected Role Lab role or pair when explicitly provided
- persisted `RoleOutputExpectationProfile`
- generic/import seed profile

All current generic profiles must keep `IsFm26Specific=false`. A profile can only become FM26-specific after later validation work explicitly marks it that way. Attributes remain supporting evidence only.

Player Profile v1 leads with role-output profile expectations. Attribute values can appear only in the support section.

Role Lab templates are also generic/import-ready by default. `IsOfficialFm26Role=false` unless a future FM26 mapping milestone validates and explicitly marks a role.
