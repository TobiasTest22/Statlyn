using System;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Statlyn.Core;
using Statlyn.DataProviders;

namespace Statlyn.Data.Persistence
{
    public sealed class DataSourceRepository : SqliteRepository
    {
        public DataSourceRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public long Save(SourceMetadata metadata, DataCompletenessReport completeness)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"INSERT INTO DataSource (
                        SourceName, ProviderType, LicenceStatus, ImportedAtUtc, Confidence, AllowedUsage, IsLive,
                        PermitsPlayerImages, PermitsProviderFlags, UsesBundledSafeFlagAssets, PermitsClubBadges, AllowsExport, DataCompleteness)
                      VALUES (
                        $sourceName, $providerType, $licenceStatus, $importedAtUtc, $confidence, $allowedUsage, $isLive,
                        $permitsPlayerImages, $permitsProviderFlags, $usesBundledSafeFlagAssets, $permitsClubBadges, $allowsExport, $dataCompleteness);";
                Add(command, "$sourceName", metadata.SourceName);
                Add(command, "$providerType", metadata.ProviderType.ToString());
                Add(command, "$licenceStatus", metadata.LicenceStatus);
                Add(command, "$importedAtUtc", metadata.ImportedAtUtc.ToString("O"));
                Add(command, "$confidence", metadata.SourceConfidence);
                Add(command, "$allowedUsage", metadata.AllowedUsage);
                Add(command, "$isLive", Bool(metadata.IsLive));
                Add(command, "$permitsPlayerImages", Bool(metadata.PermitsPlayerImages));
                Add(command, "$permitsProviderFlags", Bool(metadata.PermitsProviderFlags));
                Add(command, "$usesBundledSafeFlagAssets", Bool(metadata.UsesBundledSafeFlagAssets));
                Add(command, "$permitsClubBadges", Bool(metadata.PermitsClubBadges));
                Add(command, "$allowsExport", Bool(metadata.AllowsExport));
                Add(command, "$dataCompleteness", completeness == null ? 0 : completeness.CompletenessPercentage);
                command.ExecuteNonQuery();
                return LastInsertRowId(connection);
            }
        }

        public SourceMetadata? LoadLatest(string sourceName)
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT SourceName, ProviderType, IsLive, LicenceStatus, AllowedUsage, PermitsPlayerImages, PermitsProviderFlags,
                             UsesBundledSafeFlagAssets, PermitsClubBadges, AllowsExport, ImportedAtUtc, Confidence
                      FROM DataSource
                      WHERE SourceName = $sourceName
                      ORDER BY ImportedAtUtc DESC, Id DESC
                      LIMIT 1;";
                Add(command, "$sourceName", sourceName);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return ReadMetadata(reader);
                }
            }
        }

        public SourceMetadata? LoadLatestForPlayerSource(string sourceName)
        {
            return LoadLatest(sourceName);
        }

        private static SourceMetadata ReadMetadata(SqliteDataReader reader)
        {
            var providerType = Enum.TryParse<ProviderType>(reader.GetString(1), out var parsedProvider) ? parsedProvider : ProviderType.FutureExternalProvider;
            var imported = DateTimeOffset.TryParse(reader.GetString(10), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedImported)
                ? parsedImported
                : DateTimeOffset.UtcNow;

            return new SourceMetadata(
                reader.GetString(0),
                providerType,
                reader.GetInt32(2) != 0,
                !string.IsNullOrWhiteSpace(reader.GetString(3)),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt32(5) != 0,
                reader.GetInt32(6) != 0,
                reader.GetInt32(7) != 0,
                reader.GetInt32(8) != 0,
                reader.GetInt32(9) != 0,
                imported,
                reader.GetInt32(11));
        }
    }
}
