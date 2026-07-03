# Scout Report Safety

Scout reports are qualitative human observations. They are not a channel for hidden FM values, raw provider fields or blocked-field recovery.

## Allowed

Allowed examples:

- `Looks composed under pressure.`
- `Needs another watch for defensive recovery.`
- `Seems competitive and alert.`
- `Creates separation before shooting.`
- `Defends crosses well when contested.`

These are human observations. They do not claim exact hidden attributes.

## Redacted

`ScoutTextSanitizer` redacts hidden-looking numeric assignments before report text is stored.

Examples:

- `CurrentAbility: 200`
- `CA 155`
- `PA=180`
- `Professionalism: 20`
- `Pressure = 18`
- `Consistency 17`
- `InjuryProneness: 19`
- `ImportantMatches=16`

The sanitizer preserves ordinary qualitative language such as `he looks professional` or `handles pressure well`.

## Not Stored

Scout Desk does not store:

- raw provider snapshots
- raw blocked values
- hidden FM26 values
- exact CA or PA
- hidden personality numbers
- hidden-value-derived report fields
- fake live FM26 data
- unlicensed images, badges or flags

## Recommendations

Scout recommendations are workflow labels:

- StrongTarget
- Shortlist
- ScoutFurther
- Watchlist
- DevelopmentTarget
- Reject
- NotForRole
- TooRisky
- Unclear

They can update shortlist status only when the submit request asks for that update. They do not create transfer decisions or automatic sign actions.

## Mental And Character Notes

Mental/character ratings are scout observations only. They should describe visible behavior and confidence level, not exact hidden values.

Good:

- `Stayed focused after losing possession.`
- `Communicated clearly with teammates.`

Not allowed:

- `Professionalism: 20`
- `Pressure 18`
- `Consistency=17`
