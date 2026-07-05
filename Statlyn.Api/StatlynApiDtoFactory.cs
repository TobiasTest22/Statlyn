using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statlyn.Data;
using Statlyn.Data.Benchmarks;
using Statlyn.Data.Dashboard;
using Statlyn.Data.Fm26Snapshots;
using Statlyn.Data.PlayerIntelligence;
using Statlyn.Data.Profile;
using Statlyn.Data.Readiness;
using Statlyn.Data.Recruitment;
using Statlyn.Data.RoleLab;
using Statlyn.Data.Scouting;
using Statlyn.Data.Shortlists;
using Statlyn.Data.Workflow;
using Statlyn.DataProviders.Fm26;
using Statlyn.DataProviders.Fm26.MemoryMaps;
using Statlyn.DataProviders.Fm26.Snapshots;
using IntelligenceModels = Statlyn.Analytics.PlayerIntelligence;

namespace Statlyn.Api
{
    public sealed class StatlynApiDtoFactory
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;
        private readonly SafeFm26ConnectorService _connectorService;
        private readonly MemoryMapRegistryLoader _memoryMapLoader;
        private readonly MemoryMapSelector _memoryMapSelector;
        private readonly SafeFm26SnapshotService _snapshotService;
        private readonly Fm26SnapshotHistoryService _snapshotHistoryService;

        public StatlynApiDtoFactory(StatlynDbConnectionFactory connectionFactory)
            : this(connectionFactory, new SafeFm26ConnectorService(new NullFm26NativeConnector()), MemoryMapRegistryLoader.FromAppBase(AppContext.BaseDirectory))
        {
        }

        public StatlynApiDtoFactory(StatlynDbConnectionFactory connectionFactory, SafeFm26ConnectorService connectorService)
            : this(connectionFactory, connectorService, MemoryMapRegistryLoader.FromAppBase(AppContext.BaseDirectory))
        {
        }

        public StatlynApiDtoFactory(StatlynDbConnectionFactory connectionFactory, SafeFm26ConnectorService connectorService, MemoryMapRegistryLoader memoryMapLoader)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _connectorService = connectorService ?? throw new ArgumentNullException(nameof(connectorService));
            _memoryMapLoader = memoryMapLoader ?? throw new ArgumentNullException(nameof(memoryMapLoader));
            _memoryMapSelector = new MemoryMapSelector();
            _snapshotService = new SafeFm26SnapshotService(_connectorService, _memoryMapLoader, _memoryMapSelector, new Fm26SnapshotGateEvaluator());
            _snapshotHistoryService = new Fm26SnapshotHistoryService(_connectionFactory);
        }

        public AppHealthDto GetHealth()
        {
            var diagnostics = new StatlynDatabaseDiagnosticsService(_connectionFactory).ReadDiagnostics();
            var connector = _connectorService.GetDiagnostic();
            var registry = _memoryMapLoader.Load();
            var selection = _memoryMapSelector.Select(registry, connector.Process);
            return new AppHealthDto(
                "ok",
                "Local CSV / permitted provider workspace",
                _connectionFactory.DatabasePath,
                diagnostics.SchemaVersion,
                false,
                connector.IsNativeConnectorAvailable ? "Native connector diagnostics available. FM26 unsupported until validated maps exist." : "Native connector diagnostics unavailable. FM26 unsupported until validated maps exist.",
                selection.SupportMessage,
                "C# API is running. No live FM26 data is exposed.");
        }

        public Fm26ConnectorStatusDto GetConnectorStatus()
        {
            var diagnostic = _connectorService.GetDiagnostic();
            var registry = _memoryMapLoader.Load();
            var selection = _memoryMapSelector.Select(registry, diagnostic.Process);
            return MapConnectorStatus(diagnostic, registry, selection);
        }

        public MemoryMapRegistryDto GetMemoryMaps()
        {
            var connector = _connectorService.GetDiagnostic();
            var registry = _memoryMapLoader.Load();
            var selection = _memoryMapSelector.Select(registry, connector.Process);
            return MapMemoryMapRegistry(registry, selection);
        }

        public Fm26SnapshotDto GetFm26Snapshot()
        {
            var result = _snapshotService.CreateSnapshot();
            return MapFm26Snapshot(result.Snapshot);
        }

        public Fm26SnapshotCreateResultDto CreateFm26Snapshot()
        {
            var result = _snapshotService.CreateSnapshot();
            var history = _snapshotHistoryService.SaveSnapshot(result.Snapshot);
            return new Fm26SnapshotCreateResultDto(
                history.Success,
                history.SafeMessage,
                history.Snapshot == null ? null : MapPersistedFm26Snapshot(history.Snapshot),
                history.TotalCount,
                history.Warnings,
                history.Errors);
        }

        public Fm26SnapshotLookupDto GetLatestPersistedFm26Snapshot()
        {
            var history = _snapshotHistoryService.GetLatestSnapshot();
            return new Fm26SnapshotLookupDto(
                history.Snapshot != null,
                history.SafeMessage,
                history.Snapshot == null ? null : MapPersistedFm26Snapshot(history.Snapshot),
                history.Warnings,
                history.Errors);
        }

        public Fm26SnapshotHistoryDto GetFm26SnapshotHistory(int limit)
        {
            var history = _snapshotHistoryService.ListSnapshots(limit);
            return MapFm26SnapshotHistory(history);
        }

        public Fm26SnapshotLookupDto GetPersistedFm26Snapshot(string snapshotId)
        {
            var history = _snapshotHistoryService.GetSnapshotById(snapshotId ?? string.Empty);
            return new Fm26SnapshotLookupDto(
                history.Snapshot != null,
                history.SafeMessage,
                history.Snapshot == null ? null : MapPersistedFm26Snapshot(history.Snapshot),
                history.Warnings,
                history.Errors);
        }

        public DashboardOverviewDto GetDashboard()
        {
            var overview = new DashboardStatusService(_connectionFactory).BuildOverview();
            return new DashboardOverviewDto(
                overview.ToSafeText(),
                overview.DatabasePath,
                overview.DataSourceCount,
                overview.ImportedPlayersCount,
                overview.ShortlistCount,
                overview.ScoutAssignmentCount,
                overview.RoleLabTemplateCount,
                overview.BenchmarkDefinitionCount,
                overview.LocalReadinessStatus,
                overview.Fm26Status);
        }

        public IReadOnlyList<PlayerListItemDto> GetPlayers()
        {
            return LoadRecruitmentRows().Players.Select(MapPlayer).ToList();
        }

        public PlayerProfileDto GetPlayer(string id)
        {
            var profile = new PlayerProfileQueryService(_connectionFactory).Query(new PlayerProfileQuery { StatlynPlayerId = id ?? string.Empty });
            if (!profile.Success || profile.Player == null)
            {
                return new PlayerProfileDto(
                    false,
                    profile.SafeMessage,
                    id ?? string.Empty,
                    string.Empty,
                    "Unknown",
                    string.Empty,
                    "Not scored",
                    null,
                    null,
                    "Unknown",
                    "No benchmark yet.",
                    new List<string>(),
                    profile.Warnings,
                    profile.Diagnostics);
            }

            return new PlayerProfileDto(
                true,
                profile.SafeMessage,
                profile.Player.StatlynPlayerId,
                profile.Player.DisplayName,
                ResolvePrimaryPosition(profile),
                profile.SourceMetadata == null ? string.Empty : profile.SourceMetadata.SourceName,
                profile.LatestRoleScore == null ? "Not scored" : profile.LatestRoleScore.RoleName,
                profile.LatestRoleScore == null ? (int?)null : profile.LatestRoleScore.RoleFit,
                profile.LatestRoleScore == null ? (int?)null : profile.LatestRoleScore.Confidence,
                profile.TacticalFitDisplay,
                profile.BenchmarkSummary == null ? "No benchmark yet." : profile.BenchmarkSummary.SafeMessage,
                profile.RoleOutputSummary == null ? new List<string>() : profile.RoleOutputSummary.CoreMetrics.Concat(profile.RoleOutputSummary.SupportingMetrics).Select(metric => metric.ToString()).ToList(),
                profile.Warnings,
                profile.Diagnostics);
        }

        public PlayerIntelligenceReadinessDto GetPlayerIntelligenceReadiness()
        {
            return MapReadiness(new PlayerIntelligenceService(_connectionFactory).GetReadiness());
        }

        public PlayerIntelligenceDto GetPlayerIntelligence(string id)
        {
            return MapIntelligence(new PlayerIntelligenceService(_connectionFactory).GetIntelligence(id ?? string.Empty));
        }

        public PlayerSkillRadarDto GetPlayerRadar(string id)
        {
            return MapRadar(new PlayerIntelligenceService(_connectionFactory).GetRadar(id ?? string.Empty));
        }

        public PlayerPer90SummaryDto GetPlayerPer90(string id)
        {
            return MapPer90(new PlayerIntelligenceService(_connectionFactory).GetPer90(id ?? string.Empty));
        }

        public PlayerHeatmapDto GetPlayerHeatmap(string id)
        {
            return MapHeatmap(new PlayerIntelligenceService(_connectionFactory).GetHeatmap(id ?? string.Empty));
        }

        public PlayerSimilarityDto GetPlayerSimilar(string id)
        {
            return MapSimilarity(new PlayerIntelligenceService(_connectionFactory).GetSimilar(id ?? string.Empty));
        }

        public PlayerFitProjectionDto GetPlayerFit(string id)
        {
            return MapFit(new PlayerIntelligenceService(_connectionFactory).GetFit(id ?? string.Empty));
        }

        public PlayerValueEstimateDto GetPlayerValue(string id)
        {
            return MapValue(new PlayerIntelligenceService(_connectionFactory).GetValue(id ?? string.Empty));
        }

        public LeagueAverageComparisonDto GetPlayerLeagueComparison(string id)
        {
            return MapLeagueComparison(new PlayerIntelligenceService(_connectionFactory).GetLeagueComparison(id ?? string.Empty));
        }

        public RecruitmentBoardDto GetRecruitmentBoard()
        {
            var result = LoadRecruitmentRows();
            var players = result.Players.Select(MapPlayer).ToList();
            var recommendations = result.Players.Select(row => new RecruitmentRecommendationDto(
                row.StatlynPlayerId,
                row.DisplayName,
                row.Recommendation.HasValue ? row.Recommendation.Value.ToString() : "ScoutFurther",
                row.RoleFit.HasValue ? "C# recruitment board row uses persisted safe role evidence." : "No stored role score yet; scout further.",
                row.RoleFit,
                row.Confidence)).ToList();

            return new RecruitmentBoardDto(result.SafeMessage, result.TotalCount, players, recommendations);
        }

        public RoleLabSummaryDto GetRoleLab()
        {
            var page = new RoleLabWorkflowService(_connectionFactory).BuildPageViewModel(includeArchived: false);
            return new RoleLabSummaryDto(
                page.SafeMessage,
                page.Roles.Count,
                page.RolePairs.Count,
                page.PhaseOptions,
                page.Roles.Select(role => role.RoleName).ToList());
        }

        public IReadOnlyList<SquadGapDto> GetSquadGaps()
        {
            var overview = new DashboardStatusService(_connectionFactory).BuildOverview();
            if (overview.ImportedPlayersCount == 0)
            {
                return new[]
                {
                    new SquadGapDto("All", "No players imported; squad gap analysis is awaiting local data.", 0, "Awaiting local data.")
                };
            }

            return new[]
            {
                new SquadGapDto("All", "Define squad targets before claiming a recruitment gap.", 20, "Squad gap engine is ready for safe local data.")
            };
        }

        public ComparisonSummaryDto GetComparisons()
        {
            var players = GetPlayers().Take(2).Select(player => player.DisplayName).ToList();
            return new ComparisonSummaryDto(
                players.Count < 2 ? "No comparison available until at least two safe players are imported." : "Comparison summary uses safe player DTOs only.",
                players,
                players.Count < 2 ? new[] { "No players imported or insufficient comparison set." } : new List<string>());
        }

        public IReadOnlyList<ScoutReportSummaryDto> GetScoutReports()
        {
            var page = new ScoutDeskWorkflowService(_connectionFactory).BuildPageViewModel(new ScoutDeskQuery());
            return page.Assignments.Select(card => new ScoutReportSummaryDto(
                card.StatlynPlayerId,
                card.PlayerName,
                card.AssignmentStatus,
                card.LatestReportRecommendation,
                card.ScoutConfidence,
                card.LatestReportSummary,
                card.NoLiveFm26Label)).ToList();
        }

        public DataSourceStatusDto GetDataSources()
        {
            var overview = new DashboardStatusService(_connectionFactory).BuildOverview();
            var fixture = new UnityFixtureCsvPathResolver().Resolve(AppContext.BaseDirectory, System.IO.Path.Combine(AppContext.BaseDirectory, "StreamingAssets"));
            return new DataSourceStatusDto(
                overview.DataSourceStatus,
                "Local CSV only",
                overview.DataSourceCount,
                fixture.Success ? "Synthetic fixture available." : "Synthetic fixture missing; enter a local CSV manually.",
                overview.ImportedPlayersCount == 0 ? "No players imported." : overview.ImportedPlayersCount.ToString(CultureInfo.InvariantCulture) + " player(s) imported.",
                fixture.Success ? new List<string>() : new[] { fixture.Message });
        }

        public DiagnosticsDto GetDiagnostics()
        {
            var readiness = new LocalProductReadinessService(_connectionFactory, AppContext.BaseDirectory, System.IO.Path.Combine(AppContext.BaseDirectory, "StreamingAssets")).Run();
            var connector = _connectorService.GetDiagnostic();
            var registry = _memoryMapLoader.Load();
            var selection = _memoryMapSelector.Select(registry, connector.Process);
            return new DiagnosticsDto(
                readiness.SafeSummary,
                readiness.Success,
                readiness.DatabasePath,
                readiness.FixturePath,
                readiness.SchemaVersion,
                readiness.ImportedPlayerCount,
                readiness.ShortlistCount,
                readiness.ScoutReportCount,
                readiness.RoleLabTemplateCount,
                readiness.BenchmarkDefinitionCount,
                connector.IsNativeConnectorAvailable
                    ? "Connector diagnostics available. " + connector.SupportStatusMessage + " " + selection.SupportMessage + " No live FM26 data."
                    : "Connector diagnostics unavailable. " + connector.SupportStatusMessage + " " + selection.SupportMessage + " No live FM26 data.",
                readiness.Warnings,
                readiness.Errors);
        }

        private static PlayerIntelligenceReadinessDto MapReadiness(IntelligenceModels.PlayerIntelligenceReadiness readiness)
        {
            return new PlayerIntelligenceReadinessDto(
                readiness.Available,
                readiness.SafeMessage,
                readiness.ImportedPlayers,
                readiness.EventLocationRows,
                readiness.MarketContextRows,
                readiness.TeamStyleRows,
                readiness.LeagueAverageRows,
                readiness.StyleVectorRows,
                readiness.Warnings);
        }

        private static PlayerIntelligenceDto MapIntelligence(IntelligenceModels.PlayerIntelligenceResult result)
        {
            return new PlayerIntelligenceDto(
                MapProfile(result.Profile),
                MapRadar(result.Radar),
                MapPer90(result.Per90),
                MapHeatmap(result.Heatmap),
                MapValue(result.ValueEstimate),
                MapFit(result.FitProjection),
                MapArchetype(result.Archetype),
                MapSimilarity(result.SimilarPlayers),
                MapLeagueComparison(result.LeagueComparison),
                MapRoleAssessment(result.RoleAssessment));
        }

        private static PlayerIntelligenceProfileDto MapProfile(IntelligenceModels.PlayerIntelligenceProfile profile)
        {
            return new PlayerIntelligenceProfileDto(
                profile.Available,
                profile.SafeMessage,
                profile.StatlynPlayerId,
                profile.DisplayName,
                profile.Position,
                profile.Role,
                profile.Source,
                profile.Age,
                profile.Nationality,
                profile.DataQuality,
                profile.Confidence,
                profile.RoleFit,
                profile.Warnings,
                profile.RequiredFieldsMissing);
        }

        private static PlayerSkillRadarDto MapRadar(IntelligenceModels.PlayerSkillRadar radar)
        {
            return new PlayerSkillRadarDto(
                radar.Available,
                radar.SafeMessage,
                radar.ProfileType,
                radar.DataQuality,
                radar.Confidence,
                radar.RequiredFieldsMissing,
                radar.Warnings,
                radar.Axes.Select(MapRadarAxis).ToList());
        }

        private static PlayerRadarAxisDto MapRadarAxis(IntelligenceModels.PlayerRadarAxis axis)
        {
            return new PlayerRadarAxisDto(
                axis.AxisKey,
                axis.Label,
                axis.Value,
                axis.BenchmarkValue,
                axis.SourceMetric,
                axis.DataQuality,
                axis.Confidence);
        }

        private static PlayerPer90SummaryDto MapPer90(IntelligenceModels.PlayerPer90Summary per90)
        {
            return new PlayerPer90SummaryDto(
                per90.Available,
                per90.SafeMessage,
                per90.DataQuality,
                per90.Confidence,
                per90.RequiredFieldsMissing,
                per90.Warnings,
                per90.Metrics.Select(metric => new PlayerPer90MetricDto(
                    metric.MetricKey,
                    metric.Label,
                    metric.Value,
                    metric.Unit,
                    metric.Minutes,
                    metric.DataQuality,
                    metric.Confidence)).ToList());
        }

        private static PlayerHeatmapDto MapHeatmap(IntelligenceModels.PlayerHeatmapSummary heatmap)
        {
            return new PlayerHeatmapDto(
                heatmap.Available,
                heatmap.SafeMessage,
                heatmap.DataQuality,
                heatmap.Confidence,
                heatmap.RequiredFieldsMissing,
                heatmap.Warnings,
                heatmap.Points.Select(point => new PlayerHeatmapPointDto(
                    point.MatchId,
                    point.Minute,
                    point.X,
                    point.Y,
                    point.ActionType,
                    point.Confidence)).ToList());
        }

        private static PlayerValueEstimateDto MapValue(IntelligenceModels.PlayerValueEstimate estimate)
        {
            return new PlayerValueEstimateDto(
                estimate.Available,
                estimate.SafeMessage,
                estimate.FairValueLow,
                estimate.FairValueMid,
                estimate.FairValueHigh,
                estimate.Currency,
                estimate.ValueIndex,
                estimate.Confidence,
                estimate.DataQuality,
                estimate.KeyValueDrivers,
                estimate.KeyDiscountDrivers,
                estimate.MissingInputs,
                estimate.ModelVersion);
        }

        private static PlayerFitProjectionDto MapFit(IntelligenceModels.PlayerFitProjection fit)
        {
            return new PlayerFitProjectionDto(
                fit.Available,
                fit.SafeMessage,
                fit.DataQuality,
                fit.Confidence,
                fit.RoleFitSummary,
                fit.TeamStyleSummary,
                fit.RequiredFieldsMissing,
                fit.Warnings);
        }

        private static PlayerArchetypeDto MapArchetype(IntelligenceModels.PlayerArchetypeResult archetype)
        {
            return new PlayerArchetypeDto(
                archetype.Available,
                archetype.SafeMessage,
                archetype.Archetype,
                archetype.DataQuality,
                archetype.Confidence,
                archetype.EvidenceMetrics,
                archetype.RequiredFieldsMissing,
                archetype.Warnings);
        }

        private static PlayerSimilarityDto MapSimilarity(IntelligenceModels.PlayerSimilarityResult similarity)
        {
            return new PlayerSimilarityDto(
                similarity.Available,
                similarity.SafeMessage,
                similarity.DataQuality,
                similarity.Confidence,
                similarity.RequiredFieldsMissing,
                similarity.Warnings,
                similarity.Candidates.Select(candidate => new SimilarPlayerCandidateDto(
                    candidate.StatlynPlayerId,
                    candidate.DisplayName,
                    candidate.Role,
                    candidate.SimilarityScore,
                    candidate.Confidence,
                    candidate.DataQuality)).ToList());
        }

        private static LeagueAverageComparisonDto MapLeagueComparison(IntelligenceModels.LeagueAverageComparison comparison)
        {
            return new LeagueAverageComparisonDto(
                comparison.Available,
                comparison.SafeMessage,
                comparison.LeagueKey,
                comparison.ComparisonGroup,
                comparison.SampleSize,
                comparison.DataQuality,
                comparison.Confidence,
                comparison.RequiredFieldsMissing,
                comparison.Warnings,
                comparison.Comparisons.Select(MapRadarAxis).ToList());
        }

        private static RoleSpecificAssessmentDto MapRoleAssessment(IntelligenceModels.RoleSpecificAssessment assessment)
        {
            return new RoleSpecificAssessmentDto(
                assessment.Available,
                assessment.SafeMessage,
                assessment.RoleName,
                assessment.DataQuality,
                assessment.Confidence,
                assessment.RequiredFieldsMissing,
                assessment.Warnings,
                assessment.Definition == null ? null : MapRoleDefinition(assessment.Definition));
        }

        private static RoleParameterDefinitionDto MapRoleDefinition(IntelligenceModels.RoleParameterDefinition definition)
        {
            return new RoleParameterDefinitionDto(
                definition.RoleName,
                definition.RoleFamily,
                definition.PrimaryMetrics.Select(MapRoleMetric).ToList(),
                definition.SecondaryMetrics.Select(MapRoleMetric).ToList(),
                definition.RiskMetrics.Select(MapRoleMetric).ToList(),
                definition.StyleTraits,
                definition.MinimumMinutes,
                definition.UnavailableConditions);
        }

        private static RoleParameterMetricDto MapRoleMetric(IntelligenceModels.RoleParameterMetric metric)
        {
            return new RoleParameterMetricDto(metric.MetricKey, metric.Label, metric.Category, metric.Required, metric.MinimumMinutes);
        }

        private static Fm26ConnectorStatusDto MapConnectorStatus(Fm26ConnectorDiagnostic diagnostic, MemoryMapRegistryDiagnostic registry, MemoryMapSelectionResult selection)
        {
            return new Fm26ConnectorStatusDto(
                diagnostic.IsNativeConnectorAvailable,
                diagnostic.Availability.ToString(),
                diagnostic.ConnectorVersion,
                diagnostic.ConnectorBuildInfo,
                diagnostic.IsWindows,
                diagnostic.Process.IsDetected,
                diagnostic.Process.DetectionStatus,
                diagnostic.Process.DetectionStatusMessage,
                diagnostic.Process.ProcessDetectedAtUtc.HasValue ? diagnostic.Process.ProcessDetectedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture) : string.Empty,
                diagnostic.Process.ProcessName,
                diagnostic.Process.ProcessId,
                diagnostic.Process.ExecutableFileName,
                diagnostic.Process.ExecutableDirectorySafeLabel,
                diagnostic.Process.ProcessPath,
                diagnostic.Process.ProductName,
                diagnostic.Process.ProductVersion,
                diagnostic.Process.FileVersion,
                diagnostic.Process.Architecture,
                diagnostic.Process.Is64BitProcess,
                diagnostic.Process.ReadOnlyAccessAttempted,
                diagnostic.Process.HasReadOnlyAccess,
                diagnostic.ReadOnlyAccessStatus,
                diagnostic.Process.RequiredAccessLevel,
                diagnostic.IsFm26Supported,
                diagnostic.BuildSupportStatus,
                diagnostic.BuildSupportMessage,
                selection.SupportStatus,
                selection.SupportMessage,
                diagnostic.SupportStatusMessage,
                selection.NextActionSafeMessage,
                diagnostic.LastErrorSafeMessage,
                diagnostic.GeneratedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                diagnostic.SafeMessage,
                registry.RegistryStatus,
                selection.SelectedMapId,
                selection.HasValidatedMap,
                registry.MapsFoundCount,
                registry.UsableMapsCount,
                registry.TemplateMapsCount,
                registry.InvalidMapsCount,
                diagnostic.Warnings.Concat(new[] { registry.SafeMessage, selection.SupportMessage }).ToList(),
                diagnostic.Errors);
        }

        private static MemoryMapRegistryDto MapMemoryMapRegistry(MemoryMapRegistryDiagnostic registry, MemoryMapSelectionResult selection)
        {
            return new MemoryMapRegistryDto(
                registry.RegistryStatus,
                registry.MapsFoundCount,
                registry.UsableMapsCount,
                registry.TemplateMapsCount,
                registry.InvalidMapsCount,
                registry.HasValidatedMap,
                selection.SelectedMapId,
                selection.SelectedMapDisplayName,
                selection.SupportStatus,
                selection.SupportStatus,
                selection.SupportMessage,
                selection.NextActionSafeMessage,
                registry.SafeMessage,
                registry.Maps.Select(MapMemoryMap).ToList());
        }

        private static MemoryMapDiagnosticDto MapMemoryMap(MemoryMapFileDiagnostic map)
        {
            return new MemoryMapDiagnosticDto(
                map.MapId,
                map.DisplayName,
                map.GameVersion,
                map.BuildNumber,
                map.Platform,
                map.Architecture,
                map.IsTemplate,
                map.IsValidated,
                map.IsUsable,
                map.SupportStatus,
                map.FieldCount,
                map.VisibleFieldCount,
                map.HiddenFieldCountBlocked,
                map.SafeMessage,
                map.ValidationWarnings,
                map.ValidationErrors);
        }

        private static Fm26SnapshotDto MapFm26Snapshot(Fm26SafeSnapshot snapshot)
        {
            var summary = snapshot.SourceSummary;
            var capability = snapshot.CapabilityReport;
            return new Fm26SnapshotDto(
                snapshot.SnapshotId,
                snapshot.GeneratedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                snapshot.Status.ToString(),
                snapshot.SafeMessage,
                summary.ConnectorAvailability,
                summary.IsNativeConnectorAvailable,
                summary.PlatformStatus,
                summary.IsWindows,
                summary.ProcessDetected,
                summary.ProcessStatus,
                summary.ProcessName,
                summary.ProcessId,
                summary.ProductVersion,
                summary.FileVersion,
                summary.Architecture,
                summary.ReadOnlyAccessStatus,
                summary.MemoryMapRegistryStatus,
                summary.MapsFound,
                summary.ValidatedMaps,
                summary.TemplateMaps,
                summary.InvalidMaps,
                new Fm26SelectedMapSummaryDto(
                    summary.SelectedMapId,
                    summary.SelectedMapDisplayName,
                    summary.SelectedMapBuild,
                    summary.SelectedMapStatus),
                capability.AllGatesPassed,
                capability.BlockingGate,
                capability.IsFm26Supported,
                capability.IsLiveReadingAvailable,
                capability.ReaderStatus,
                capability.FieldPolicyStatus,
                snapshot.Gates.Select(gate => new Fm26SnapshotGateDto(
                    gate.GateKey,
                    gate.Label,
                    gate.GateStatus.ToString(),
                    gate.SnapshotStatus.ToString(),
                    gate.SafeMessage,
                    gate.NextActionSafeMessage)).ToList(),
                snapshot.BlockReasons.Select(reason => new Fm26SnapshotBlockReasonDto(
                    reason.GateKey,
                    reason.Reason,
                    reason.SafeMessage,
                    reason.NextActionSafeMessage)).ToList(),
                snapshot.NextActionSafeMessage,
                snapshot.Warnings,
                snapshot.Errors);
        }

        private static Fm26SnapshotHistoryDto MapFm26SnapshotHistory(Fm26SnapshotHistoryResult history)
        {
            var latest = history.Snapshot ?? history.Snapshots.FirstOrDefault();
            return new Fm26SnapshotHistoryDto(
                history.Success,
                history.SafeMessage,
                history.TotalCount,
                latest == null ? null : MapPersistedFm26Snapshot(latest),
                history.Snapshots.Select(MapFm26SnapshotSummary).ToList(),
                history.Warnings,
                history.Errors);
        }

        private static Fm26SnapshotSummaryDto MapFm26SnapshotSummary(PersistedFm26SnapshotRecord snapshot)
        {
            return new Fm26SnapshotSummaryDto(
                snapshot.SnapshotId,
                snapshot.GeneratedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                snapshot.SnapshotStatus,
                snapshot.ConnectorAvailability,
                snapshot.ProcessStatus,
                snapshot.ReadOnlyAccessStatus,
                snapshot.MemoryMapRegistryStatus,
                snapshot.BlockingGate,
                snapshot.LiveReadingAllowed,
                snapshot.NextActionSafeMessage,
                snapshot.WarningCount,
                snapshot.ErrorCount);
        }

        private static Fm26PersistedSnapshotDto MapPersistedFm26Snapshot(PersistedFm26SnapshotRecord snapshot)
        {
            return new Fm26PersistedSnapshotDto(
                snapshot.SnapshotId,
                snapshot.GeneratedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                snapshot.SnapshotStatus,
                snapshot.SafeMessage,
                snapshot.ConnectorAvailability,
                snapshot.PlatformStatus,
                snapshot.ProcessDetected,
                snapshot.ProcessStatus,
                snapshot.ReadOnlyAccessStatus,
                snapshot.MemoryMapRegistryStatus,
                snapshot.MapsFound,
                snapshot.ValidatedMaps,
                snapshot.TemplateMaps,
                snapshot.InvalidMaps,
                new Fm26SelectedMapSummaryDto(
                    snapshot.SelectedMapId,
                    snapshot.SelectedMapDisplayName,
                    snapshot.SelectedMapBuild,
                    snapshot.MemoryMapRegistryStatus),
                snapshot.AllGatesPassed,
                snapshot.BlockingGate,
                snapshot.LiveReadingAllowed,
                snapshot.NextActionSafeMessage,
                snapshot.WarningCount,
                snapshot.ErrorCount,
                snapshot.Gates.Select(gate => new Fm26SnapshotGateDto(
                    gate.GateKey,
                    gate.GateName,
                    gate.Status,
                    snapshot.SnapshotStatus,
                    gate.SafeMessage,
                    string.Empty)).ToList());
        }

        private RecruitmentCentreResult LoadRecruitmentRows()
        {
            return new RecruitmentCentreQueryService(_connectionFactory).Query(new RecruitmentCentreQuery { Limit = 100 });
        }

        private static string ResolvePrimaryPosition(PlayerProfileResult profile)
        {
            var field = profile.VisibleFields.FirstOrDefault(item => string.Equals(item.FieldName, "PrimaryPosition", StringComparison.OrdinalIgnoreCase));
            if (field != null && !string.IsNullOrWhiteSpace(field.DisplayValue))
            {
                return field.DisplayValue;
            }

            var fact = profile.MaskedPlayer != null && profile.MaskedPlayer.Facts.TryGetValue("PrimaryPosition", out var value)
                ? value
                : null;
            return fact != null && fact.IsKnown && !string.IsNullOrWhiteSpace(fact.Value) ? fact.Value : "Unknown";
        }

        private static PlayerListItemDto MapPlayer(RecruitmentCentrePlayerRow row)
        {
            return new PlayerListItemDto(
                row.StatlynPlayerId,
                row.DisplayName,
                row.AgeDisplay,
                row.Nationality,
                row.PositionGroup,
                row.PrimaryPosition,
                row.SourceName,
                row.SourceConfidence,
                row.DataCompleteness,
                row.LatestRoleName,
                row.RoleFit,
                row.Confidence,
                row.Recommendation.HasValue ? row.Recommendation.Value.ToString() : "ScoutFurther",
                row.MissingDataCount,
                row.BlockedFieldCount,
                row.BenchmarkIndicator.Status,
                row.KeyWarnings);
        }
    }
}
