# React/Tauri UI

`Statlyn.Desktop` is the strategic desktop UI. It is a React + Tauri professional analyst workspace with a black glass visual direction, dense tables, safe status cards, loading states, empty states, error states and an API status indicator.

The desktop UI may handle:

- navigation
- layout
- display DTOs
- API calls
- filtering and sorting over already-safe DTOs
- local UI state

The desktop UI must not contain:

- recruitment scoring
- role scoring
- benchmark calculations
- scouting firewall rules
- hidden-value filtering
- provider mapping
- FM memory logic
- direct database access

Run it with:

```powershell
cd Statlyn.Desktop
npm install
npm run dev
```

Start `Statlyn.Api` separately:

```powershell
dotnet run --project ..\Statlyn.Api\Statlyn.Api.csproj --urls http://localhost:5118
```

The default API URL is `http://localhost:5118`. Override it with `VITE_STATLYN_API_URL` if needed.
