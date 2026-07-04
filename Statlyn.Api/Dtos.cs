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
        string ProcessName,
        int? ProcessId,
        string ProcessPath,
        string ProductVersion,
        string Architecture,
        string ReadOnlyAccessStatus,
        bool IsFm26Supported,
        string SupportStatusMessage,
        string LastErrorSafeMessage,
        string GeneratedAtUtc,
        string SafeMessage);

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
