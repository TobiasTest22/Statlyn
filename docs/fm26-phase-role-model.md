# FM26-Style Phase Role Model

Statlyn's Role Lab uses an FM26-style phase model without claiming official FM26 mappings.

## Phases

`TacticalPhase`:

- InPossession
- OutOfPossession
- DualPhasePair

In-possession roles describe movement, build-up and final-third behaviour. Out-of-possession roles describe pressing, block behaviour, screening, recovery and compactness. Role pairs connect one IP role with one OOP role so a player can be analysed across shape changes.

## Families

Role families include goalkeeper, centre-back, full-back/wing-back, midfield, wide attacker and forward families, plus phase-specific defensive and attacking families such as high press, mid block, recovery cover, build-up, chance creation and goal threat.

Families are modelling categories. They are not hidden FM role ids and not final FM26 role declarations.

## Slots

`TacticalSlot` stores formation context such as GK, centre-back slots, full-back/wing-back slots, defensive midfield, central midfield, attacking midfield, wide slots and striker.

Role pairs can use different IP and OOP slots. Example: a player can occupy an attacking wide slot in possession and recover into a central or wide defensive slot out of possession.

## Output First

Role Lab metric requirements lead the profile:

- Core
- Important
- Useful
- ContextOnly

Missing core output lowers confidence. It is never filled with fake zero values.

Attributes remain supporting evidence only. Role Lab does not become an attribute-first or hidden-value model.

## FM26 Validation

Future FM26 validation can map a tactical role to a verified FM26 role id. Until then:

- `IsOfficialFm26Role=false`
- `Source=BuiltInSeed` or `UserCreated`
- no role is presented as official FM26
- no live FM26 memory map is assumed
- no old duty model is used as the foundation

This keeps Role Lab useful for import-safe analysis now while leaving room for validated FM26 support later.
