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

            var rowsRead = playersResult.Value.Count;
            var rowsAccepted = 0;
            var rowsRejected = 0;
            var fieldsStored = 0;
            var playerStatsStored = 0;
            var physicalMetricsStored = 0;
            var blockedFields = 0;
            var unknownFields = 0;

            try
            {
                using (var connection = _connectionFactory.OpenConnection())
                using (var transaction = connection.BeginTransaction())
                {
                    _dataSources.Save(metadata, completeness, connection, transaction);

                    foreach (var raw in playersResult.Value)
                    {
                        MaskedPlayer masked;
                        RoleScore roleScore;
                        try
                        {
                            masked = _firewall.Mask(raw);
                            roleScore = _scoring.ScorePlayer(masked, options.PreviewRole);
                        }
                        catch (Exception ex)
                        {
                            rowsRejected++;
                            diagnostics.Add("import.player.failed", DiagnosticStatus.Partial, "A player row failed safe import before persistence.", ex.GetType().Name);
                            continue;
                        }

                        var playerId = _players.Save(masked, metadata, completeness, connection, transaction);
                        DeleteCurrentPlayerSnapshot(playerId, masked.StatlynPlayerId, connection, transaction);
                        fieldsStored += _visibleFields.SaveFields(playerId, masked, connection, transaction);
                        playerStatsStored += _playerStats.SaveFromFields(playerId, masked, connection, transaction);
                        physicalMetricsStored += _physicalMetrics.SaveFromFields(playerId, masked, connection, transaction);
                        _roleScores.Save(playerId, roleScore, connection, transaction);
                        blockedFields += _blockedFields.SaveBlockedFields(masked.StatlynPlayerId, masked, connection, transaction);
                        unknownFields += masked.BlockedFields.Count(field => field.Key == PlayerFieldKey.Unknown);
                        _profileSnapshots.Save(playerId, metadata.SourceName, IsFixture(metadata), metadata.IsLive && metadata.ProviderType == ProviderType.FM26LiveMemory, roleScore.Confidence, completeness.CompletenessPercentage, connection, transaction);
                        rowsAccepted++;

                        if (options.FatalFailureAfterAcceptedRows >= 0 && rowsAccepted >= options.FatalFailureAfterAcceptedRows)
                        {
                            throw new InvalidOperationException("Simulated fatal import failure.");
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
                    _importAudits.Save(audit, connection, transaction);
                    transaction.Commit();

                    return new ImportPipelineResult(rowsRead, rowsAccepted, rowsRejected, fieldsStored, playerStatsStored, physicalMetricsStored, blockedFields, unknownFields, diagnostics);
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add("import.fatal", DiagnosticStatus.Failed, "Fatal import failure rolled back persisted data.", ex.GetType().Name);
                _importAudits.Save(new ImportAuditRecord(
                    metadata.SourceName,
                    metadata.ProviderType.ToString(),
                    DateTimeOffset.UtcNow,
                    rowsRead,
                    0,
                    rowsRead,
                    0,
                    0,
                    0,
                    0,
                    unknownFields,
                    SafeDiagnostics(diagnostics)));
                return new ImportPipelineResult(rowsRead, 0, rowsRead, 0, 0, 0, 0, unknownFields, diagnostics);
            }
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
                DiagnosticSanitizer.Sanitize(SafeDiagnostics(diagnostics) + " " + (message ?? string.Empty))));
            return new ImportPipelineResult(0, 0, 0, 0, 0, 0, 0, 0, diagnostics);
        }

        private void DeleteCurrentPlayerSnapshot(long playerId, string statlynPlayerId, Microsoft.Data.Sqlite.SqliteConnection connection, Microsoft.Data.Sqlite.SqliteTransaction transaction)
        {
            _visibleFields.DeleteForPlayer(playerId, connection, transaction);
            _playerStats.DeleteForPlayer(playerId, connection, transaction);
            _physicalMetrics.DeleteForPlayer(playerId, connection, transaction);
            _roleScores.DeleteForPlayer(playerId, connection, transaction);
            _blockedFields.DeleteForEntity(statlynPlayerId, connection, transaction);
            _profileSnapshots.DeleteForPlayer(playerId, connection, transaction);
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

            return DiagnosticSanitizer.Sanitize(builder.ToString());
        }
    }
}
