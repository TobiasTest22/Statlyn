# Frontend Safety Rules

The React/Tauri frontend is a display and workflow surface. C# is the decision brain.

Frontend code must not:

- import or reimplement C# decision engines
- calculate recruitment scores
- calculate role fit
- calculate benchmark percentiles
- run the scouting firewall
- inspect provider raw data
- open SQLite directly
- call native connector code
- display fake live FM26 status
- display hidden or raw blocked values

Frontend empty states should use honest language:

- `Awaiting local data`
- `No players imported`
- `Insufficient sample`
- `Connector unsupported`
- `No validated FM map`

If the API is offline, the UI should show a readable error state and ask the user to start `Statlyn.Api`.
