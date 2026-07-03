using System;
using System.IO;
using System.Linq;
using System.Text;
using Statlyn.Core;
using Statlyn.Data;
using Statlyn.Data.Workflow;
using Statlyn.DataProviders.Import;
using Xunit;

namespace Statlyn.Tests
{
    public sealed class Milestone18Tests
    {
        [Fact]
        public void CsvPreviewDetectsRowsAndColumnsWithoutPersisting()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var preview = PreviewFixture();

            Assert.True(preview.Success);
            Assert.True(preview.FileReadable);
            Assert.Equal(2, preview.RowsDetected);
            Assert.Contains("Finishing", preview.Columns);
            Assert.Contains("CurrentAbility", preview.Columns);
            Assert.Equal(0, CountRows(factory, "DataSource"));
            Assert.Equal(0, CountRows(factory, "Player"));
            Assert.Equal(0, CountRows(factory, "VisibleField"));
        }

        [Fact]
        public void CsvPreviewMapsKnownFootballFields()
        {
            var preview = PreviewFixture();

            AssertMapping(preview, "Finishing", PlayerFieldKey.TechnicalAttribute, "Finishing");
            AssertMapping(preview, "xG", PlayerFieldKey.PlayerStat, "xG");
            AssertMapping(preview, "TopSpeed", PlayerFieldKey.PhysicalData, "TopSpeed");
            Assert.True(preview.MappedCount >= 10);
        }

        [Fact]
        public void CsvPreviewMarksForbiddenAndUnknownColumnsSafely()
        {
            var path = CreateCsv("SourcePlayerId,DisplayName,CurrentAbility,Professionalism,MysteryMetric", "one,Synthetic One,987,654,42");
            var preview = Preview(path, CreateFixtureMetadata());

            Assert.True(preview.Success);
            Assert.True(preview.ColumnMappings.Single(column => column.SourceColumn == "CurrentAbility").IsForbidden);
            Assert.True(preview.ColumnMappings.Single(column => column.SourceColumn == "Professionalism").IsForbidden);
            Assert.True(preview.ColumnMappings.Single(column => column.SourceColumn == "MysteryMetric").IsUnknown);
            Assert.Contains("CurrentAbility", preview.ForbiddenFields);
            Assert.Contains("Professionalism", preview.ForbiddenFields);
            Assert.Contains("MysteryMetric", preview.UnknownFields);

            var safeText = PreviewText(preview);
            Assert.DoesNotContain("987", safeText);
            Assert.DoesNotContain("654", safeText);
        }

        [Fact]
        public void ForbiddenRawNameOverridesExplicitPreviewMapping()
        {
            var path = CreateCsv("CurrentAbility", "987");
            var mappings = new FieldMappingSet(new[] { new FieldMapping("CurrentAbility", PlayerFieldKey.TechnicalAttribute, "Finishing", FieldValueKind.Number) });
            var preview = new CsvPreviewService().Preview(path, mappings, CreateFixtureMetadata());
            var column = preview.ColumnMappings.Single();

            Assert.True(column.IsForbidden);
            Assert.Equal(PlayerFieldKey.CurrentAbility, column.FieldKey);
            Assert.False(column.IsMapped);
        }

        [Fact]
        public void CsvPreviewMarksPermissionBlockedMediaAndLicensedMetrics()
        {
            var path = CreateCsv("SourcePlayerId,DisplayName,Image,NationalityFlag,ClubBadge,xG", "one,Synthetic One,face.png,RO,badge.png,0.4");
            var preview = Preview(path, new SourceMetadata(
                "Unlicensed CSV",
                ProviderType.Csv,
                false,
                false,
                "not licensed",
                "preview only",
                false,
                false,
                true,
                false,
                false,
                DateTimeOffset.UtcNow,
                40));

            Assert.Equal("PermissionBlocked", ColumnPreviewViewModel.From(preview.ColumnMappings.Single(column => column.SourceColumn == "Image")).Status);
            Assert.Equal("Safe", ColumnPreviewViewModel.From(preview.ColumnMappings.Single(column => column.SourceColumn == "NationalityFlag")).Status);
            Assert.Equal("PermissionBlocked", ColumnPreviewViewModel.From(preview.ColumnMappings.Single(column => column.SourceColumn == "ClubBadge")).Status);
            Assert.Equal("PermissionBlocked", ColumnPreviewViewModel.From(preview.ColumnMappings.Single(column => column.SourceColumn == "xG")).Status);
        }

        [Fact]
        public void WorkflowPreviewsAndImportsSyntheticCsvWithDatabaseDiagnostics()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var workflow = new DataSourceImportWorkflowService(factory);

            var preview = workflow.Preview(CreateRequest());
            var result = workflow.Import(CreateRequest());

            Assert.True(preview.Success);
            Assert.True(result.Success);
            Assert.NotNull(result.ImportResult);
            Assert.NotNull(result.DatabaseDiagnostics);
            Assert.Equal(2, result.ImportResult!.RowsRead);
            Assert.Equal(2, result.ImportResult.RowsAccepted);
            Assert.True(result.ImportResult.FieldsStored > 0);
            Assert.True(result.ImportResult.PlayerStatsStored >= 6);
            Assert.True(result.ImportResult.PhysicalMetricsStored >= 4);
            Assert.Equal(2, result.DatabaseDiagnostics!.PlayersCount);
            Assert.True(result.DatabaseDiagnostics.PlayerStatsCount >= 6);
            Assert.Equal(4, result.DatabaseDiagnostics.BlockedAuditCount);
        }

        [Fact]
        public void WorkflowReturnsSafeWarningsForUnlicensedAndForbiddenFields()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var request = CreateRequest();
            request.IsLicensed = false;
            request.AllowedUsage = string.Empty;

            var result = new DataSourceImportWorkflowService(factory).Preview(request);
            var text = WorkflowText(result);

            Assert.Contains(result.WarningMessages, warning => warning.Contains("not marked licensed", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.WarningMessages, warning => warning.Contains("Allowed usage", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.PreviewViewModel.ColumnRows, row => row.SourceColumn == "CurrentAbility" && row.Status == "Forbidden");
            Assert.Contains(result.PreviewViewModel.ColumnRows, row => row.SourceColumn == "Professionalism" && row.Status == "Forbidden");
            Assert.DoesNotContain("CurrentAbility: 200", text);
            Assert.DoesNotContain("Professionalism:20", text);
            Assert.DoesNotContain("PlayerRawSnapshot", text);
            Assert.DoesNotContain("RawFieldValue", text);
        }

        [Fact]
        public void WorkflowDoubleImportRemainsIdempotent()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var workflow = new DataSourceImportWorkflowService(factory);

            workflow.Import(CreateRequest());
            var second = workflow.Import(CreateRequest());

            Assert.True(second.Success);
            Assert.Equal(2, CountRows(factory, "Player"));
            Assert.Equal(2, CountWhere(factory, "PlayerStat", "StatName = 'xG'"));
            Assert.Equal(4, CountRows(factory, "BlockedFieldAudit"));
            Assert.Equal(2, second.DatabaseDiagnostics!.PlayersCount);
            Assert.Equal(2, CountWhere(factory, "VisibleField", "FieldName = 'Finishing'"));
        }

        [Fact]
        public void ImportResultViewModelShowsCountsAndNoHiddenValues()
        {
            using var factory = RuntimeDatabaseFactory.CreateInMemory();
            var result = new DataSourceImportWorkflowService(factory).Import(CreateRequest());
            var viewModel = result.ImportResultViewModel!;
            var text = WorkflowText(result);

            Assert.True(viewModel.Success);
            Assert.Equal(2, viewModel.RowsRead);
            Assert.Equal(2, viewModel.RowsAccepted);
            Assert.Equal(0, viewModel.RowsRejected);
            Assert.Equal(2, viewModel.DatabasePlayersCount);
            Assert.True(viewModel.BlockedFields >= 4);
            Assert.DoesNotContain("CurrentAbility 200", text);
            Assert.DoesNotContain("Professionalism 20", text);
            Assert.DoesNotContain("PlayerRawSnapshot", text);
        }

        [Fact]
        public void ForbiddenFieldsAppearByCategoryAndNameOnlyInViewModels()
        {
            var path = CreateCsv("SourcePlayerId,DisplayName,CurrentAbility", "one,Synthetic One,987");
            var preview = DataSourcePreviewViewModel.From(Preview(path, CreateFixtureMetadata()), Array.Empty<string>(), Array.Empty<string>());
            var forbidden = preview.ColumnRows.Single(row => row.SourceColumn == "CurrentAbility");

            Assert.Equal("Forbidden", forbidden.Status);
            Assert.Equal("CurrentAbility", forbidden.Category);
            Assert.Equal("CurrentAbility", forbidden.MappedTo);
            Assert.DoesNotContain("987", forbidden.Reason);
        }

        [Fact]
        public void DatabasePathResolverAndRuntimeFactorySupportDefaultFileAndInMemory()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "statlyn-path-" + Guid.NewGuid().ToString("N"));
            var resolved = new StatlynDatabasePathResolver().ResolveDefaultPath(tempRoot);

            Assert.False(string.IsNullOrWhiteSpace(resolved));
            Assert.EndsWith(Path.Combine("Statlyn", "statlyn.db"), resolved, StringComparison.OrdinalIgnoreCase);

            using (var memory = RuntimeDatabaseFactory.CreateInMemory())
            {
                Assert.StartsWith("in-memory:", memory.DatabasePath, StringComparison.OrdinalIgnoreCase);
                Assert.Equal(StatlynSchemaVersion.Current, new StatlynDatabaseDiagnosticsService(memory).ReadDiagnostics().SchemaVersion);
            }

            using (var file = RuntimeDatabaseFactory.CreateDefault(tempRoot))
            {
                Assert.True(File.Exists(file.DatabasePath));
                Assert.Equal(StatlynSchemaVersion.Current, new StatlynDatabaseDiagnosticsService(file).ReadDiagnostics().SchemaVersion);
            }
        }

        private static CsvPreviewResult PreviewFixture()
        {
            return Preview(FixturePath(), CreateFixtureMetadata());
        }

        private static CsvPreviewResult Preview(string path, SourceMetadata metadata)
        {
            return new CsvPreviewService().Preview(path, new FieldMappingSet(Array.Empty<FieldMapping>()), metadata);
        }

        private static DataSourceImportRequest CreateRequest()
        {
            return new DataSourceImportRequest
            {
                CsvPath = FixturePath(),
                SourceName = "Synthetic CSV fixture",
                LicenceStatus = "synthetic test fixture",
                AllowedUsage = "development fixture only",
                IsLicensed = true,
                SourceConfidence = 80,
                PermitsPlayerImages = false,
                PermitsProviderFlags = false,
                UsesBundledSafeFlagAssets = true,
                PermitsClubBadges = false,
                AllowsExport = true
            };
        }

        private static SourceMetadata CreateFixtureMetadata()
        {
            return new SourceMetadata(
                "Synthetic CSV fixture",
                ProviderType.Csv,
                false,
                true,
                "synthetic test fixture",
                "development fixture only",
                false,
                false,
                true,
                false,
                true,
                DateTimeOffset.UtcNow,
                80);
        }

        private static string FixturePath()
        {
            return Path.Combine(AppContext.BaseDirectory, "Fixtures", "players.sample.csv");
        }

        private static string CreateCsv(string header, string row)
        {
            var directory = Path.Combine(Path.GetTempPath(), "statlyn-preview-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "preview.csv");
            File.WriteAllText(path, header + Environment.NewLine + row + Environment.NewLine);
            return path;
        }

        private static void AssertMapping(CsvPreviewResult preview, string sourceColumn, PlayerFieldKey key, string fieldName)
        {
            var mapping = preview.ColumnMappings.Single(column => column.SourceColumn == sourceColumn);
            Assert.True(mapping.IsMapped);
            Assert.Equal(key, mapping.FieldKey);
            Assert.Equal(fieldName, mapping.FieldName);
        }

        private static string PreviewText(CsvPreviewResult preview)
        {
            var builder = new StringBuilder();
            builder.Append(preview.SourceName).Append(' ').Append(preview.FilePath).Append(' ');
            foreach (var mapping in preview.ColumnMappings)
            {
                builder.Append(mapping.SourceColumn).Append(' ')
                    .Append(mapping.FieldName).Append(' ')
                    .Append(mapping.FieldKey).Append(' ')
                    .Append(mapping.Reason).Append(' ');
            }

            foreach (var item in preview.Diagnostics.Items)
            {
                builder.Append(item.Message).Append(' ').Append(item.TechnicalDetail).Append(' ');
            }

            return builder.ToString();
        }

        private static string WorkflowText(DataSourceImportWorkflowResult result)
        {
            var builder = new StringBuilder();
            builder.Append(result.SafeMessage).Append(' ')
                .Append(result.PreviewViewModel.SafeMessage).Append(' ');

            foreach (var warning in result.WarningMessages)
            {
                builder.Append(warning).Append(' ');
            }

            foreach (var error in result.ErrorMessages)
            {
                builder.Append(error).Append(' ');
            }

            foreach (var row in result.PreviewViewModel.ColumnRows)
            {
                builder.Append(row.SourceColumn).Append(' ')
                    .Append(row.MappedTo).Append(' ')
                    .Append(row.Category).Append(' ')
                    .Append(row.Status).Append(' ')
                    .Append(row.Reason).Append(' ');
            }

            if (result.ImportResultViewModel != null)
            {
                builder.Append(result.ImportResultViewModel.SafeMessage).Append(' ');
            }

            return builder.ToString();
        }

        private static int CountRows(StatlynDbConnectionFactory factory, string table)
        {
            return CountWhere(factory, table, "1 = 1");
        }

        private static int CountWhere(StatlynDbConnectionFactory factory, string table, string where)
        {
            using var connection = factory.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM " + table + " WHERE " + where + ";";
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}
