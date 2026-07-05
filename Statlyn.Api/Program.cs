using Statlyn.Api;
using Statlyn.Data;
using Statlyn.DataProviders.Fm26;
using Statlyn.DataProviders.Fm26.MemoryMaps;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
    });
});
builder.Services.AddSingleton(_ =>
{
    var configuredPath = builder.Configuration["Statlyn:DatabasePath"];
    var factory = string.IsNullOrWhiteSpace(configuredPath)
        ? RuntimeDatabaseFactory.CreateDefault()
        : RuntimeDatabaseFactory.CreateFile(configuredPath);
    return factory;
});
builder.Services.AddSingleton<IFm26NativeConnector, NativeFm26Connector>();
builder.Services.AddSingleton<SafeFm26ConnectorService>();
builder.Services.AddSingleton(_ =>
{
    var configuredPath = builder.Configuration["Statlyn:MemoryMapsPath"];
    return string.IsNullOrWhiteSpace(configuredPath)
        ? MemoryMapRegistryLoader.FromAppBase(AppContext.BaseDirectory)
        : new MemoryMapRegistryLoader(configuredPath);
});
builder.Services.AddSingleton<StatlynApiDtoFactory>();

var app = builder.Build();
app.UseCors();

app.MapGet("/health", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetHealth());
app.MapGet("/dashboard", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetDashboard());
app.MapGet("/players", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetPlayers());
app.MapGet("/players/{id}", (string id, StatlynApiDtoFactory dtoFactory) => dtoFactory.GetPlayer(id));
app.MapGet("/analytics/player-intelligence/readiness", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetPlayerIntelligenceReadiness());
app.MapGet("/players/{id}/intelligence", (string id, StatlynApiDtoFactory dtoFactory) => dtoFactory.GetPlayerIntelligence(id));
app.MapGet("/players/{id}/radar", (string id, StatlynApiDtoFactory dtoFactory) => dtoFactory.GetPlayerRadar(id));
app.MapGet("/players/{id}/per90", (string id, StatlynApiDtoFactory dtoFactory) => dtoFactory.GetPlayerPer90(id));
app.MapGet("/players/{id}/heatmap", (string id, StatlynApiDtoFactory dtoFactory) => dtoFactory.GetPlayerHeatmap(id));
app.MapGet("/players/{id}/similar", (string id, StatlynApiDtoFactory dtoFactory) => dtoFactory.GetPlayerSimilar(id));
app.MapGet("/players/{id}/fit", (string id, StatlynApiDtoFactory dtoFactory) => dtoFactory.GetPlayerFit(id));
app.MapGet("/players/{id}/value", (string id, StatlynApiDtoFactory dtoFactory) => dtoFactory.GetPlayerValue(id));
app.MapGet("/players/{id}/league-comparison", (string id, StatlynApiDtoFactory dtoFactory) => dtoFactory.GetPlayerLeagueComparison(id));
app.MapGet("/recruitment-board", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetRecruitmentBoard());
app.MapGet("/role-lab", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetRoleLab());
app.MapGet("/squad-gaps", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetSquadGaps());
app.MapGet("/comparisons", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetComparisons());
app.MapGet("/scout-reports", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetScoutReports());
app.MapGet("/data-sources", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetDataSources());
app.MapGet("/diagnostics", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetDiagnostics());
app.MapGet("/connector/status", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetConnectorStatus());
app.MapGet("/connector/fm26", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetConnectorStatus());
app.MapGet("/connector/fm26/snapshot", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetFm26Snapshot());
app.MapGet("/diagnostics/fm26", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetConnectorStatus());
app.MapGet("/diagnostics/fm26/summary", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetConnectorStatus());
app.MapGet("/diagnostics/fm26/snapshot", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetFm26Snapshot());
app.MapGet("/diagnostics/fm26/snapshot/readiness", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetFm26Snapshot());
app.MapPost("/diagnostics/fm26/snapshots", (StatlynApiDtoFactory dtoFactory) => dtoFactory.CreateFm26Snapshot());
app.MapGet("/diagnostics/fm26/snapshots/latest", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetLatestPersistedFm26Snapshot());
app.MapGet("/diagnostics/fm26/snapshots", (int? limit, StatlynApiDtoFactory dtoFactory) => dtoFactory.GetFm26SnapshotHistory(limit ?? 20));
app.MapGet("/diagnostics/fm26/snapshots/{snapshotId}", (string snapshotId, StatlynApiDtoFactory dtoFactory) => dtoFactory.GetPersistedFm26Snapshot(snapshotId));
app.MapGet("/diagnostics/memory-maps", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetMemoryMaps());
app.MapGet("/connector/memory-maps", (StatlynApiDtoFactory dtoFactory) => dtoFactory.GetMemoryMaps());

app.Run();

public partial class Program
{
}
