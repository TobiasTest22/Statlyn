# Shortlists

Milestone 2.2 adds Shortlists v1, the first persisted recruitment decision workflow. Milestone 2.3 connects Shortlists to Scout Desk assignments.

## Flow

The supported loop is:

```text
Import CSV
-> Recruitment Centre
-> Player Profile
-> Add to Shortlist
-> Shortlists page
-> status / priority / follow-up tracking
-> Create scout assignment
-> Submit qualitative scout report
-> optional shortlist status update
```

Shortlists use persisted safe SQLite data only. Players are added by `StatlynPlayerId`, not by raw provider objects.

## Data Stored

`Shortlist` stores:

- name
- description
- created and updated timestamps
- active/archived state

`ShortlistPlayer` stores:

- persisted `ShortlistId`
- persisted `PlayerId`
- persisted `StatlynPlayerId`
- status
- priority
- follow-up action
- sanitized role name
- recommendation label
- added reason
- user note
- created and updated timestamps

The same player cannot duplicate inside the same shortlist. Adding the player again refreshes safe workflow labels.

## Scout Desk Action

Each shortlist player row has `Create Scout Assignment`. The action passes the persisted `ShortlistPlayerId` and `StatlynPlayerId` into `ScoutDeskWorkflowService`.

Defaults come from the shortlist row:

- role name
- priority
- shortlist link

The Shortlists page also shows the latest scout report recommendation, confidence, date and short safe summary. If no report exists, it displays `No scout report yet.`

## Workflow Labels

Statuses include:

- Longlist
- Watchlist
- ScoutFurther
- Shortlist
- StrongTarget
- DevelopmentTarget
- LoanTarget
- FreeAgentTarget
- Rejected
- BadFit
- TooRisky
- TooExpensive
- NotForRole

Priorities include Low, Medium, High and Urgent.

Follow-up actions include None, ScoutAgain, WatchMore, CompareAlternatives, CheckAvailability, CheckWage, CheckMedical, CheckWorkPermit, ReviewRoleFit and Reject.

These are recruitment workflow labels. They are not FM hidden values and do not depend on hidden CA, hidden PA or hidden personality data.

## Add From Recruitment Centre

Recruitment Centre cards include `Add to Main Recruitment List`. The action passes only the safe row view model and `StatlynPlayerId` into `ShortlistWorkflowService`.

The card can show a `Shortlisted` badge when the player belongs to an active shortlist.

## Add From Player Profile

Player Profile includes a shortlist membership panel and `Add to Main Recruitment List`.

`ShortlistDecisionHelper` suggests default labels from visible profile context:

- low confidence prefers ScoutFurther
- missing core output prefers ScoutFurther or Watchlist
- strong visible role fit and confidence can suggest Shortlist or StrongTarget
- blocked-field counts add warnings without exposing raw values
- no path automatically stores a Sign decision

## Safety Rules

Shortlists do not store:

- raw provider snapshots
- raw blocked values
- hidden FM26 values
- hidden CA or PA
- hidden personality fields
- fake live FM26 data
- scraped or external API data
- unlicensed images, badges or flags

User notes are user-entered workflow text and are sanitized for hidden-value-looking patterns before storage.

Scout report summaries shown on Shortlists are qualitative local notes. Exact hidden-looking assignments such as `CA 155`, `PA=180` or `Professionalism: 20` are redacted before storage and display.

## Unity Status

The Unity Shortlists page can create shortlists, view overview cards, view player details, create Scout Desk assignments, show latest scout report summary, update status/priority/follow-up/user note and remove players.

Unity Editor validation remains manual unless a release note explicitly says the Editor was opened and checked. SQLite-in-Unity loading should be confirmed with the Data Sources runtime check after running `tools/copy-managed-to-unity.ps1`.
