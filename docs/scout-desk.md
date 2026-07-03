# Scout Desk

Milestone 2.3 adds Scout Desk v1, the first persisted human scouting workflow. Milestone 2.4 lets Scout Desk use Role Lab scout questions when an assignment role name matches a persisted tactical role.

## Flow

```text
Import CSV
-> Recruitment Centre
-> Player Profile
-> Shortlist
-> Scout Assignment
-> Scout Report
-> Scout Recommendation
-> optional Shortlist status/follow-up update
```

Scout Desk is local-only and uses persisted safe SQLite data. It does not scrape, call external APIs, use FotMob, query live FM26 data or create fake players.

## Assignments

Assignments can be created from:

- a persisted `StatlynPlayerId`
- a persisted shortlist player row

Shortlist-created assignments inherit the shortlist role and priority. Assignments store the persisted player link, optional shortlist links, status, priority, assigned-to text, source name and timestamps.

## Reports

Scout reports store qualitative human observations:

- technical rating
- tactical rating
- physical rating
- mental/character observation rating
- recommendation
- confidence
- strengths
- weaknesses
- risks
- follow-up action
- final summary
- role/output question answers

Ratings are scout observations, not hidden FM attributes. Scout recommendations can support recruitment decisions, but Scout Desk never signs players automatically.

## Questions

`ScoutQuestionGenerator` creates prompts from position, missing output, blocked-field counts and source confidence.

If an assignment role matches a Role Lab tactical role, Scout Desk includes that role's persisted scout questions. If no match exists, it falls back to generated prompts.

Examples:

- missing striker xG: shooting-position observation
- missing wide xA/key passes: chance creation from wide or half-space zones
- missing centre-back aerial output: crosses and aerial duels
- missing goalkeeper save output: saves beyond routine stops
- low source confidence: source trust question
- blocked fields: observe behavior directly and do not infer hidden values

Questions are prompts for observation, not requests for exact CA, PA or hidden personality values.

Role Lab questions follow the same rule. They are sanitized before display and storage.

## Unity Page

The Unity Scout Desk page includes:

- official Statlyn header branding
- assignment creation form
- create-for-first-shortlisted action
- assignment cards
- assignment detail
- generated scout questions
- qualitative report form
- optional linked shortlist update toggle
- report history
- no-live-FM26 and qualitative safety copy

## Current Limits

- Reports are local-only.
- Assigned-to is plain text, not a user account system.
- There is no external scouting provider integration.
- There is no live FM26 support.
- There are no fake benchmark percentiles.
- Player images, club badges and provider flags are not used.
