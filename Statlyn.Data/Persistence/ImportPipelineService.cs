using System;
using System.Linq;
using System.Text;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.Core.Diagnostics;
using Statlyn.DataProviders;
using Statlyn.Scouting;

namespace Statlyn.Data.Persistence
{
    public sealed class ImportPipelineService
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;
        private readonly DataSourceRepository _dataSources;
        private readonly PlayerRepository _players;
        private readonly VisibleFieldRepository _visibleFields;
        private readonly PlayerStatRepository _playerStats;
        private readonly PhysicalMetricRepository _physicalMetrics;
        private readonly RoleScoreRepository _roleScores;
        private readonly BlockedFieldAuditRepository _blockedFields;
        private readonly ImportAuditRepository _importAudits;
        private readonly ProfileSnapshotRepository _profileSnapshots;
        private readonly ScoutingKnowledgeFirewall _firewall;
        private readonly RoleScoringEngine _scoring;

        public ImportPipelineService(StatlynDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _dataSources = new DataSourceRepository(connectionFactory);
            _players = new PlayerRepository(connectionFactory);
            _visibleFields = new VisibleFieldRepository(connectionFactory);
            _playerStats = new PlayerStatRepository(connectionFactory);
            _physicalMetrics = new PhysicalMetricRepository(connectionFactory);
            _roleScores = new RoleScoreRepository(connectionFactory);
            _blockedFields = new BlockedFieldAuditRepository(connectionFactory);
            _importAudits = new ImportAuditRepository(connectionFactory);
            _profileSnapshots = new ProfileSnapshotRepository(connectionFactory);
            _firewall = new ScoutingKnowledgeFirewall();
            _scoring = new RoleScoringEngine();
        }

        public ImportPipelineResult Import(IDataProvider provider, ImportPipelineOptions options)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            options = options ?? ImportPipelineOptions.CreateDefault();
            var diagnostics = new DiagnosticReport();
            AppendDiagnostics(diagnostics, provider.ValidateAccess());

            var metadataResult = provider.ReadSourceMetadata();
            AppendDiagnostics(diagnostics, metadataResult.Diagnostics);
            if (!metadataResult.Success || metadataResult.Value == null)
            {
                diagnostics.Add("import.metadata", DiagnosticStatus.Failed, "Source metadata could not be read.", metadataResult.Message);
                return AuditFailure(provider, diagnostics, metadataResult.Message);
            }

            var playersResult = provider.ReadPlayers();
            AppendDiagnostics(diagnostics, playersResult.Diagnostics);
            if (!playersResult.Success || playersResult.Value == null)
            {
                diagnostics.Add("import.players", DiagnosticStatus.Failed, "Players could not be read.", playersResult.Message);
                return AuditFailure(provider, diagnostics, playersResult.Message);
            }

            var metadata = metadataResult.Value;
            var completeness = provider.GetDataCompleteness();
            _dataSources.Save(metadata, completeness);

            var rowsRead = playersResult.Value.Count;
            var rowsAccepted = 0;
            var rowsRejected = 0;
            var fieldsStored = 0;
            var playerStatsStored = 0;
            var physicalMetricsStored = 0;
            var blockedFields = 0;
            var unknownFields = 0;

            foreach (var raw in playersResult.Value)
            {
                try
                {
                    var masked = _firewall.Mask(raw);
                    var roleScore = _scoring.ScorePlayer(masked, options.PreviewRole);
                    var playerId = _players.Save(masked, metadata, completeness);
                    fieldsStored += _visibleFields.SaveFields(playerId, masked);
                    playerStatsStored += _playerStats.SaveFromFields(playerId, masked);
                    physicalMetricsStored += _physicalMetrics.SaveFromFields(playerId, masked);
                    _roleScores.Save(playerId, roleScore);
                    blockedFields += _blockedFields.SaveBlockedFields(masked.StatlynPlayerId, masked);
                    unknownFields += masked.BlockedFields.Count(field => field.Key == PlayerFieldKey.Unknown);
                    _profileSnapshots.Save(playerId, metadata.SourceName, IsFixture(metadata), metadata.IsLive && metadata.ProviderType == ProviderType.FM26LiveMemory, roleScore.Confidence, completeness.CompletenessPercentage);
                    rowsAccepted++;
                }
                catch (Exception ex)
                {
                    rowsRejected++;
                    diagnostics.Add("import.player.failed", DiagnosticStatus.Partial, "A player row failed safe import.", ex.GetType().Name);
                }
            }

            diagnostics.Add("import.complete", DiagnosticStatus.Verified, "Safe import pipeline completed.", rowsAccepted + " accepted; " + rowsRejected + " rejected.");
            var audit = new ImportAuditRecord(
                metadata.SourceName,
                metadata.ProviderType.ToString(),
                DateTimeOffset.UtcNow,
                rowsRead,
                rowsAccepted,
                rowsRejected,
                fieldsStored,
                playerStatsStored,
                physicalMetricsStored,
                blockedFields,
                unknownFields,
                SafeDiagnostics(diagnostics));
            _importAudits.Save(audit);

            return new ImportPipelineResult(rowsRead, rowsAccepted, rowsRejected, fieldsStored, playerStatsStored, physicalMetricsStored, blockedFields, unknownFields, diagnostics);
        }

        private ImportPipelineResult AuditFailure(IDataProvider provider, DiagnosticReport diagnostics, string message)
        {
            _importAudits.Save(new ImportAuditRecord(
                provider.ProviderName,
                provider.ProviderType.ToString(),
                DateTimeOffset.UtcNow,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                SafeDiagnostics(diagnostics) + " " + (message ?? string.Empty)));
            return new ImportPipelineResult(0, 0, 0, 0, 0, 0, 0, 0, diagnostics);
        }

        private static bool IsFixture(SourceMetadata metadata)
        {
            return metadata.SourceName.IndexOf("fixture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   metadata.AllowedUsage.IndexOf("fixture", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AppendDiagnostics(DiagnosticReport target, DiagnosticReport source)
        {
            foreach (var item in source.Items)
            {
                target.Add(item.Key, item.Status, item.Message, item.TechnicalDetail);
            }
        }

        private static string SafeDiagnostics(DiagnosticReport diagnostics)
        {
            var builder = new StringBuilder();
            foreach (var item in diagnostics.Items)
            {
                builder.Append(item.Key).Append(": ").Append(item.Message).Append(" ").Append(item.TechnicalDetail).AppendLine();
            }

            return builder.ToString();
        }
    }
}
