using System.Collections.Generic;
using System.Linq;
using Statlyn.Analytics;
using Statlyn.Core;
using Statlyn.DataProviders;

namespace Statlyn.Data.Persistence
{
    public sealed class PersistedMaskedPlayerLoader
    {
        private readonly DataSourceRepository _dataSources;
        private readonly PlayerRepository _players;
        private readonly VisibleFieldRepository _visibleFields;
        private readonly BlockedFieldAuditRepository _blockedFields;
        private readonly RoleScoreRepository _roleScores;
        private readonly PlayerStatRepository _playerStats;
        private readonly PhysicalMetricRepository _physicalMetrics;

        public PersistedMaskedPlayerLoader(StatlynDbConnectionFactory connectionFactory)
        {
            _dataSources = new DataSourceRepository(connectionFactory);
            _players = new PlayerRepository(connectionFactory);
            _visibleFields = new VisibleFieldRepository(connectionFactory);
            _blockedFields = new BlockedFieldAuditRepository(connectionFactory);
            _roleScores = new RoleScoreRepository(connectionFactory);
            _playerStats = new PlayerStatRepository(connectionFactory);
            _physicalMetrics = new PhysicalMetricRepository(connectionFactory);
        }

        public IReadOnlyList<StoredPlayerRecord> LoadPlayersBySource(string sourceName)
        {
            return _players.LoadBySource(sourceName);
        }

        public PersistedMaskedPlayerData? LoadByStatlynPlayerId(string statlynPlayerId, string roleName)
        {
            var record = _players.LoadByStatlynPlayerId(statlynPlayerId);
            if (record == null)
            {
                return null;
            }

            var metadata = _dataSources.LoadLatestForPlayerSource(record.SourceName);
            if (metadata == null)
            {
                return null;
            }

            var fields = _visibleFields.LoadFields(record.Id);
            var blocked = _blockedFields.LoadForEntity(record.StatlynPlayerId);
            var fieldMap = fields.ToDictionary(field => field.InstanceKey, field => field);
            var attributes = new Dictionary<string, VisibleField<int>>(System.StringComparer.OrdinalIgnoreCase);
            var facts = new Dictionary<string, VisibleField<string>>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var field in fields)
            {
                if (field.CanDisplay)
                {
                    facts[field.FieldName] = VisibleField<string>.Known(field.FieldName, field.DisplayValue, field.CanScore, field.Confidence, FieldVisibilityCategory.AlwaysVisible, field.SourceProvider);
                }

                if (field.Key == PlayerFieldKey.TechnicalAttribute && field.NumericValue.HasValue)
                {
                    attributes[field.FieldName] = VisibleField<int>.Known(field.FieldName, (int)System.Math.Round(field.NumericValue.Value), field.CanScore, field.Confidence, FieldVisibilityCategory.VisibleIfScouted, field.SourceProvider);
                }
            }

            var masked = new MaskedPlayer(
                record.StatlynPlayerId,
                record.DisplayName,
                metadata.SourceName,
                metadata.ProviderType,
                _players.LoadScoutKnowledge(record.Id),
                record.SourceConfidence,
                fieldMap,
                blocked,
                attributes,
                facts);

            var roleScore = _roleScores.LoadLatest(record.Id);
            var completeness = new DataCompletenessReport(
                record.DataCompleteness,
                100,
                roleScore == null ? new string[0] : roleScore.MissingData);

            return new PersistedMaskedPlayerData(
                record,
                metadata,
                masked,
                roleScore,
                completeness,
                _playerStats.LoadForPlayer(record.Id),
                _physicalMetrics.LoadForPlayer(record.Id));
        }

        public PersistedMaskedPlayerData? LoadByStatlynPlayerId(string statlynPlayerId)
        {
            return LoadByStatlynPlayerId(statlynPlayerId, string.Empty);
        }
    }

    public sealed class PersistedMaskedPlayerData
    {
        public PersistedMaskedPlayerData(
            StoredPlayerRecord player,
            SourceMetadata sourceMetadata,
            MaskedPlayer maskedPlayer,
            RoleScore? latestRoleScore,
            DataCompletenessReport completeness,
            IReadOnlyList<PlayerStatRecord> playerStats,
            IReadOnlyList<PhysicalMetricRecord> physicalMetrics)
        {
            Player = player;
            SourceMetadata = sourceMetadata;
            MaskedPlayer = maskedPlayer;
            LatestRoleScore = latestRoleScore;
            Completeness = completeness;
            PlayerStats = playerStats;
            PhysicalMetrics = physicalMetrics;
        }

        public StoredPlayerRecord Player { get; }

        public SourceMetadata SourceMetadata { get; }

        public MaskedPlayer MaskedPlayer { get; }

        public RoleScore? LatestRoleScore { get; }

        public DataCompletenessReport Completeness { get; }

        public IReadOnlyList<PlayerStatRecord> PlayerStats { get; }

        public IReadOnlyList<PhysicalMetricRecord> PhysicalMetrics { get; }
    }
}
