using System.Collections.Generic;

namespace Statlyn.Api
{
    public sealed record AppHealthDto(
        string Status,
        string ProductMode,
        string DatabasePath,
        int SchemaVersion,
        bool IsFm26Supported,
        string ConnectorStatus,
        string ValidatedMapStatus,
        string SafeMessage);

    public sealed record Fm26ConnectorStatusDto(
        bool IsNativeConnectorAvailable,
        string Availability,
        string ConnectorVersion,
        string ConnectorBuildInfo,
        bool IsWindows,
        bool IsFmProcessDetected,
        string DetectionStatus,
        string DetectionStatusMessage,
        string ProcessDetectedAtUtc,
        string ProcessName,
        int? ProcessId,
        string ExecutableFileName,
        string ExecutableDirectorySafeLabel,
        string ProcessPath,
        string ProductName,
        string ProductVersion,
        string FileVersion,
        string Architecture,
        bool? Is64BitProcess,
        bool ReadOnlyAccessAttempted,
        bool HasReadOnlyAccess,
        string ReadOnlyAccessStatus,
        string RequiredAccessLevel,
        bool IsFm26Supported,
        string BuildSupportStatus,
        string BuildSupportMessage,
        string MapSupportStatus,
        string MapSupportMessage,
        string SupportStatusMessage,
        string NextActionSafeMessage,
        string LastErrorSafeMessage,
        string GeneratedAtUtc,
        string SafeMessage,
        string MemoryMapRegistryStatus,
        string SelectedMemoryMapId,
        bool HasValidatedMap,
        int MemoryMapCount,
        int UsableMemoryMapCount,
        int TemplateMemoryMapCount,
        int InvalidMemoryMapCount,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors);

    public sealed record MemoryMapRegistryDto(
        string RegistryStatus,
        int MapsFoundCount,
        int UsableMapsCount,
        int TemplateMapsCount,
        int InvalidMapsCount,
        bool HasValidatedMap,
        string SelectedMapId,
        string SelectedMapDisplayName,
        string SelectedMapStatus,
        string MapSupportStatus,
        string MapSupportMessage,
        string NextActionSafeMessage,
        string SafeMessage,
        IReadOnlyList<MemoryMapDiagnosticDto> Maps);

    public sealed record MemoryMapDiagnosticDto(
        string MapId,
        string DisplayName,
        string GameVersion,
        string BuildNumber,
        string Platform,
        string Architecture,
        bool IsTemplate,
        bool IsValidated,
        bool IsUsable,
        string SupportStatus,
        int FieldCount,
        int VisibleFieldCount,
        int HiddenFieldCountBlocked,
        string SafeMessage,
        IReadOnlyList<string> ValidationWarnings,
        IReadOnlyList<string> ValidationErrors);

    public sealed record Fm26SelectedMapSummaryDto(
        string MapId,
        string DisplayName,
        string Build,
        string Status);

    public sealed record Fm26SnapshotGateDto(
        string GateKey,
        string Label,
        string GateStatus,
        string SnapshotStatus,
        string SafeMessage,
        string NextAction);

    public sealed record Fm26SnapshotBlockReasonDto(
        string GateKey,
        string Reason,
        string SafeMessage,
        string NextAction);

    public sealed record Fm26SnapshotDto(
        string SnapshotId,
        string GeneratedAtUtc,
        string SnapshotStatus,
        string SafeMessage,
        string ConnectorStatus,
        bool IsNativeConnectorAvailable,
        string PlatformStatus,
        bool IsWindows,
        bool FmProcessDetected,
        string FmProcessStatus,
        string ProcessName,
        int? ProcessId,
        string ProductVersion,
        string FileVersion,
        string Architecture,
        string ReadOnlyStatus,
        string MapRegistryStatus,
        int MapsFound,
        int ValidatedMaps,
        int TemplateMaps,
        int InvalidMaps,
        Fm26SelectedMapSummaryDto SelectedMapSummary,
        bool AllGatesPassed,
        string BlockingGate,
        bool IsFm26Supported,
        bool IsLiveReadingAvailable,
        string ReaderStatus,
        string FieldPolicyStatus,
        IReadOnlyList<Fm26SnapshotGateDto> Gates,
        IReadOnlyList<Fm26SnapshotBlockReasonDto> BlockReasons,
        string NextAction,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors);

    public sealed record Fm26SnapshotSummaryDto(
        string SnapshotId,
        string GeneratedAtUtc,
        string SnapshotStatus,
        string ConnectorStatus,
        string ProcessStatus,
        string ReadOnlyStatus,
        string MapRegistryStatus,
        string BlockingGate,
        bool LiveReadingAllowed,
        string NextAction,
        int WarningsCount,
        int ErrorsCount);

    public sealed record Fm26PersistedSnapshotDto(
        string SnapshotId,
        string GeneratedAtUtc,
        string SnapshotStatus,
        string SafeMessage,
        string ConnectorStatus,
        string PlatformStatus,
        bool ProcessDetected,
        string ProcessStatus,
        string ReadOnlyStatus,
        string MapRegistryStatus,
        int MapsFound,
        int ValidatedMaps,
        int TemplateMaps,
        int InvalidMaps,
        Fm26SelectedMapSummaryDto SelectedMapSummary,
        bool AllGatesPassed,
        string BlockingGate,
        bool LiveReadingAllowed,
        string NextAction,
        int WarningsCount,
        int ErrorsCount,
        IReadOnlyList<Fm26SnapshotGateDto> Gates);

    public sealed record Fm26SnapshotHistoryDto(
        bool Success,
        string SafeMessage,
        int TotalCount,
        Fm26PersistedSnapshotDto? LatestSnapshot,
        IReadOnlyList<Fm26SnapshotSummaryDto> Snapshots,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors);

    public sealed record Fm26SnapshotCreateResultDto(
        bool Success,
        string SafeMessage,
        Fm26PersistedSnapshotDto? Snapshot,
        int TotalCount,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors);

    public sealed record Fm26SnapshotLookupDto(
        bool Found,
        string SafeMessage,
        Fm26PersistedSnapshotDto? Snapshot,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors);

    public sealed record DashboardOverviewDto(
        string SafeMessage,
        string DatabasePath,
        int DataSourceCount,
        int ImportedPlayersCount,
        int ShortlistCount,
        int ScoutAssignmentCount,
        int RoleLabTemplateCount,
        int BenchmarkDefinitionCount,
        string LocalReadinessStatus,
        string Fm26Status);

    public sealed record PlayerListItemDto(
        string StatlynPlayerId,
        string DisplayName,
        string Age,
        string Nationality,
        string PositionGroup,
        string PrimaryPosition,
        string SourceName,
        int SourceConfidence,
        int DataCompleteness,
        string RoleName,
        int? RoleFit,
        int? Confidence,
        string Recommendation,
        int MissingDataCount,
        int BlockedFieldCount,
        string BenchmarkStatus,
        IReadOnlyList<string> SafeWarnings);

    public sealed record PlayerProfileDto(
        bool Found,
        string SafeMessage,
        string StatlynPlayerId,
        string DisplayName,
        string PrimaryPosition,
        string SourceName,
        string RoleName,
        int? RoleFit,
        int? Confidence,
        string TacticalFit,
        string BenchmarkStatus,
        IReadOnlyList<string> OutputMetrics,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Diagnostics);

    public sealed record PlayerDataAvailabilityDto(
        bool Available,
        string SafeMessage,
        string DataQuality,
        int Confidence,
        IReadOnlyList<string> MissingFields,
        IReadOnlyList<string> Warnings);

    public sealed record PlayerIntelligenceReadinessDto(
        bool Available,
        string SafeMessage,
        int ImportedPlayers,
        int EventLocationRows,
        int MarketContextRows,
        int TeamStyleRows,
        int LeagueAverageRows,
        int StyleVectorRows,
        IReadOnlyList<string> Warnings);

    public sealed record PlayerIntelligenceProfileDto(
        bool Available,
        string SafeMessage,
        string StatlynPlayerId,
        string DisplayName,
        string Position,
        string Role,
        string Source,
        int? Age,
        string Nationality,
        string DataQuality,
        int Confidence,
        int? RoleFit,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> MissingFields);

    public sealed record PlayerRadarAxisDto(
        string AxisKey,
        string Label,
        double? Value,
        double? BenchmarkValue,
        string SourceMetric,
        string DataQuality,
        int Confidence);

    public sealed record PlayerSkillRadarDto(
        bool Available,
        string SafeMessage,
        string ProfileType,
        string DataQuality,
        int Confidence,
        IReadOnlyList<string> MissingFields,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<PlayerRadarAxisDto> Axes);

    public sealed record PlayerPer90MetricDto(
        string MetricKey,
        string Label,
        double Value,
        string Unit,
        int Minutes,
        string DataQuality,
        int Confidence);

    public sealed record PlayerPer90SummaryDto(
        bool Available,
        string SafeMessage,
        string DataQuality,
        int Confidence,
        IReadOnlyList<string> MissingFields,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<PlayerPer90MetricDto> Metrics);

    public sealed record PlayerHeatmapPointDto(
        string MatchId,
        double Minute,
        double X,
        double Y,
        string ActionType,
        int Confidence);

    public sealed record PlayerHeatmapDto(
        bool Available,
        string SafeMessage,
        string DataQuality,
        int Confidence,
        IReadOnlyList<string> MissingFields,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<PlayerHeatmapPointDto> Points);

    public sealed record PlayerValueEstimateDto(
        bool Available,
        string SafeMessage,
        double? FairValueLow,
        double? FairValueMid,
        double? FairValueHigh,
        string Currency,
        double? ValueIndex,
        int Confidence,
        string DataQuality,
        IReadOnlyList<string> KeyValueDrivers,
        IReadOnlyList<string> KeyDiscountDrivers,
        IReadOnlyList<string> MissingInputs,
        string ModelVersion);

    public sealed record PlayerFitProjectionDto(
        bool Available,
        string SafeMessage,
        string DataQuality,
        int Confidence,
        string RoleFitSummary,
        string TeamStyleSummary,
        IReadOnlyList<string> MissingFields,
        IReadOnlyList<string> Warnings);

    public sealed record PlayerArchetypeDto(
        bool Available,
        string SafeMessage,
        string Archetype,
        string DataQuality,
        int Confidence,
        IReadOnlyList<string> EvidenceMetrics,
        IReadOnlyList<string> MissingFields,
        IReadOnlyList<string> Warnings);

    public sealed record SimilarPlayerCandidateDto(
        string StatlynPlayerId,
        string DisplayName,
        string Role,
        double SimilarityScore,
        int Confidence,
        string DataQuality);

    public sealed record PlayerSimilarityDto(
        bool Available,
        string SafeMessage,
        string DataQuality,
        int Confidence,
        IReadOnlyList<string> MissingFields,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<SimilarPlayerCandidateDto> Candidates);

    public sealed record LeagueAverageComparisonDto(
        bool Available,
        string SafeMessage,
        string LeagueKey,
        string ComparisonGroup,
        int SampleSize,
        string DataQuality,
        int Confidence,
        IReadOnlyList<string> MissingFields,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<PlayerRadarAxisDto> Comparisons);

    public sealed record RoleParameterMetricDto(
        string MetricKey,
        string Label,
        string Category,
        bool Required,
        int MinimumMinutes);

    public sealed record RoleParameterDefinitionDto(
        string RoleName,
        string RoleFamily,
        IReadOnlyList<RoleParameterMetricDto> PrimaryMetrics,
        IReadOnlyList<RoleParameterMetricDto> SecondaryMetrics,
        IReadOnlyList<RoleParameterMetricDto> RiskMetrics,
        IReadOnlyList<string> StyleTraits,
        int MinimumMinutes,
        IReadOnlyList<string> UnavailableConditions);

    public sealed record RoleSpecificAssessmentDto(
        bool Available,
        string SafeMessage,
        string RoleName,
        string DataQuality,
        int Confidence,
        IReadOnlyList<string> MissingFields,
        IReadOnlyList<string> Warnings,
        RoleParameterDefinitionDto? Definition);

    public sealed record PlayerIntelligenceDto(
        PlayerIntelligenceProfileDto Profile,
        PlayerSkillRadarDto Radar,
        PlayerPer90SummaryDto Per90,
        PlayerHeatmapDto Heatmap,
        PlayerValueEstimateDto ValueEstimate,
        PlayerFitProjectionDto FitProjection,
        PlayerArchetypeDto Archetype,
        PlayerSimilarityDto SimilarPlayers,
        LeagueAverageComparisonDto LeagueComparison,
        RoleSpecificAssessmentDto RoleAssessment);

    public sealed record RecruitmentBoardDto(
        string SafeMessage,
        int TotalPlayers,
        IReadOnlyList<PlayerListItemDto> Players,
        IReadOnlyList<RecruitmentRecommendationDto> Recommendations);

    public sealed record RecruitmentRecommendationDto(
        string StatlynPlayerId,
        string PlayerName,
        string Recommendation,
        string Reason,
        int? RoleFit,
        int? Confidence);

    public sealed record RoleLabSummaryDto(
        string SafeMessage,
        int RoleCount,
        int RolePairCount,
        IReadOnlyList<string> PhaseOptions,
        IReadOnlyList<string> RoleNames);

    public sealed record SquadGapDto(
        string PositionGroup,
        string NeedSummary,
        int Urgency,
        string SafeMessage);

    public sealed record ComparisonSummaryDto(
        string SafeMessage,
        IReadOnlyList<string> ComparedPlayers,
        IReadOnlyList<string> Warnings);

    public sealed record ScoutReportSummaryDto(
        string StatlynPlayerId,
        string PlayerName,
        string AssignmentStatus,
        string LatestRecommendation,
        string ScoutConfidence,
        string SafeSummary,
        string SafeNotice);

    public sealed record DataSourceStatusDto(
        string SafeMessage,
        string Mode,
        int DataSourceCount,
        string FixtureStatus,
        string ImportStatus,
        IReadOnlyList<string> Warnings);

    public sealed record DiagnosticsDto(
        string SafeSummary,
        bool Success,
        string DatabasePath,
        string FixturePath,
        int SchemaVersion,
        int ImportedPlayerCount,
        int ShortlistCount,
        int ScoutReportCount,
        int RoleLabTemplateCount,
        int BenchmarkDefinitionCount,
        string Fm26Status,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors);
}
