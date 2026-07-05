using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using Statlyn.Data.Persistence;

namespace Statlyn.Data.Fm26Snapshots
{
    public sealed class Fm26SnapshotRepository : SqliteRepository
    {
        public Fm26SnapshotRepository(StatlynDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public void SaveSnapshot(PersistedFm26SnapshotRecord snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (string.IsNullOrWhiteSpace(snapshot.SnapshotId))
            {
                throw new ArgumentException("Snapshot id is required.", nameof(snapshot));
            }

            using (var connection = ConnectionFactory.OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                SaveSnapshot(snapshot, connection, transaction);
                transaction.Commit();
            }
        }

        public PersistedFm26SnapshotRecord? GetLatestSnapshot()
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = SnapshotSelectSql + @"
                    ORDER BY generated_at_utc DESC, snapshot_id DESC
                    LIMIT 1;";
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var snapshot = ReadSnapshot(reader);
                    snapshot.Gates = LoadGates(connection, snapshot.SnapshotId);
                    return snapshot;
                }
            }
        }

        public IReadOnlyList<PersistedFm26SnapshotRecord> ListSnapshots(int limit)
        {
            var clampedLimit = Math.Max(1, Math.Min(limit, 50));
            var snapshots = new List<PersistedFm26SnapshotRecord>();

            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = SnapshotSelectSql + @"
                    ORDER BY generated_at_utc DESC, snapshot_id DESC
                    LIMIT $limit;";
                Add(command, "$limit", clampedLimit);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        snapshots.Add(ReadSnapshot(reader));
                    }
                }

                foreach (var snapshot in snapshots)
                {
                    snapshot.Gates = LoadGates(connection, snapshot.SnapshotId);
                }
            }

            return snapshots;
        }

        public PersistedFm26SnapshotRecord? GetSnapshotById(string snapshotId)
        {
            var safeId = Normalize(snapshotId);
            if (string.IsNullOrWhiteSpace(safeId))
            {
                return null;
            }

            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = SnapshotSelectSql + @"
                    WHERE snapshot_id = $snapshotId
                    LIMIT 1;";
                Add(command, "$snapshotId", safeId);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var snapshot = ReadSnapshot(reader);
                    snapshot.Gates = LoadGates(connection, snapshot.SnapshotId);
                    return snapshot;
                }
            }
        }

        public int CountSnapshots()
        {
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM fm26_snapshot_runs;";
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        public int DeleteOldSnapshots(int keepLatest)
        {
            var safeKeep = Math.Max(1, keepLatest);
            using (var connection = ConnectionFactory.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"DELETE FROM fm26_snapshot_runs
                      WHERE snapshot_id NOT IN (
                          SELECT snapshot_id
                          FROM fm26_snapshot_runs
                          ORDER BY generated_at_utc DESC, snapshot_id DESC
                          LIMIT $keepLatest
                      );";
                Add(command, "$keepLatest", safeKeep);
                return command.ExecuteNonQuery();
            }
        }

        private void SaveSnapshot(PersistedFm26SnapshotRecord snapshot, SqliteConnection connection, SqliteTransaction transaction)
        {
            var gates = snapshot.Gates ?? Array.Empty<PersistedFm26SnapshotGateRecord>();

            using (var command = CreateCommand(connection, transaction))
            {
                command.CommandText =
                    @"INSERT OR REPLACE INTO fm26_snapshot_runs (
                        snapshot_id, generated_at_utc, snapshot_status, safe_message,
                        connector_availability, platform_status, process_detected, process_status,
                        process_name, process_id, product_version, file_version, architecture,
                        read_only_access_status, memory_map_registry_status, maps_found, validated_maps,
                        template_maps, invalid_maps, selected_map_id, selected_map_display_name,
                        selected_map_build, all_gates_passed, blocking_gate, live_reading_allowed,
                        next_action_safe_message, warning_count, error_count)
                      VALUES (
                        $snapshotId, $generatedAtUtc, $snapshotStatus, $safeMessage,
                        $connectorAvailability, $platformStatus, $processDetected, $processStatus,
                        $processName, $processId, $productVersion, $fileVersion, $architecture,
                        $readOnlyAccessStatus, $memoryMapRegistryStatus, $mapsFound, $validatedMaps,
                        $templateMaps, $invalidMaps, $selectedMapId, $selectedMapDisplayName,
                        $selectedMapBuild, $allGatesPassed, $blockingGate, $liveReadingAllowed,
                        $nextActionSafeMessage, $warningCount, $errorCount);";
                Add(command, "$snapshotId", Normalize(snapshot.SnapshotId));
                Add(command, "$generatedAtUtc", snapshot.GeneratedAtUtc.ToString("O", CultureInfo.InvariantCulture));
                Add(command, "$snapshotStatus", Normalize(snapshot.SnapshotStatus));
                Add(command, "$safeMessage", SafeText(snapshot.SafeMessage));
                Add(command, "$connectorAvailability", Normalize(snapshot.ConnectorAvailability));
                Add(command, "$platformStatus", Normalize(snapshot.PlatformStatus));
                Add(command, "$processDetected", Bool(snapshot.ProcessDetected));
                Add(command, "$processStatus", SafeText(snapshot.ProcessStatus));
                Add(command, "$processName", SafeText(snapshot.ProcessName));
                Add(command, "$processId", snapshot.ProcessId);
                Add(command, "$productVersion", SafeText(snapshot.ProductVersion));
                Add(command, "$fileVersion", SafeText(snapshot.FileVersion));
                Add(command, "$architecture", SafeText(snapshot.Architecture));
                Add(command, "$readOnlyAccessStatus", SafeText(snapshot.ReadOnlyAccessStatus));
                Add(command, "$memoryMapRegistryStatus", SafeText(snapshot.MemoryMapRegistryStatus));
                Add(command, "$mapsFound", Math.Max(0, snapshot.MapsFound));
                Add(command, "$validatedMaps", Math.Max(0, snapshot.ValidatedMaps));
                Add(command, "$templateMaps", Math.Max(0, snapshot.TemplateMaps));
                Add(command, "$invalidMaps", Math.Max(0, snapshot.InvalidMaps));
                Add(command, "$selectedMapId", SafeText(snapshot.SelectedMapId));
                Add(command, "$selectedMapDisplayName", SafeText(snapshot.SelectedMapDisplayName));
                Add(command, "$selectedMapBuild", SafeText(snapshot.SelectedMapBuild));
                Add(command, "$allGatesPassed", Bool(snapshot.AllGatesPassed));
                Add(command, "$blockingGate", SafeText(snapshot.BlockingGate));
                Add(command, "$liveReadingAllowed", Bool(snapshot.LiveReadingAllowed));
                Add(command, "$nextActionSafeMessage", SafeText(snapshot.NextActionSafeMessage));
                Add(command, "$warningCount", Math.Max(0, snapshot.WarningCount));
                Add(command, "$errorCount", Math.Max(0, snapshot.ErrorCount));
                command.ExecuteNonQuery();
            }

            using (var delete = CreateCommand(connection, transaction))
            {
                delete.CommandText = "DELETE FROM fm26_snapshot_gate_results WHERE snapshot_id = $snapshotId;";
                Add(delete, "$snapshotId", Normalize(snapshot.SnapshotId));
                delete.ExecuteNonQuery();
            }

            var sortOrder = 0;
            foreach (var gate in gates.OrderBy(gate => gate.SortOrder).ThenBy(gate => gate.GateKey, StringComparer.OrdinalIgnoreCase))
            {
                using (var command = CreateCommand(connection, transaction))
                {
                    command.CommandText =
                        @"INSERT INTO fm26_snapshot_gate_results (
                            snapshot_id, gate_key, gate_name, status, safe_message, sort_order)
                          VALUES (
                            $snapshotId, $gateKey, $gateName, $status, $safeMessage, $sortOrder);";
                    Add(command, "$snapshotId", Normalize(snapshot.SnapshotId));
                    Add(command, "$gateKey", Normalize(gate.GateKey));
                    Add(command, "$gateName", SafeText(gate.GateName));
                    Add(command, "$status", Normalize(gate.Status));
                    Add(command, "$safeMessage", SafeText(gate.SafeMessage));
                    Add(command, "$sortOrder", sortOrder);
                    command.ExecuteNonQuery();
                }

                sortOrder++;
            }
        }

        private IReadOnlyList<PersistedFm26SnapshotGateRecord> LoadGates(SqliteConnection connection, string snapshotId)
        {
            var gates = new List<PersistedFm26SnapshotGateRecord>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText =
                    @"SELECT snapshot_id, gate_key, gate_name, status, safe_message, sort_order
                      FROM fm26_snapshot_gate_results
                      WHERE snapshot_id = $snapshotId
                      ORDER BY sort_order ASC, gate_key ASC;";
                Add(command, "$snapshotId", snapshotId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        gates.Add(new PersistedFm26SnapshotGateRecord
                        {
                            SnapshotId = ReadString(reader, 0),
                            GateKey = ReadString(reader, 1),
                            GateName = ReadString(reader, 2),
                            Status = ReadString(reader, 3),
                            SafeMessage = ReadString(reader, 4),
                            SortOrder = reader.GetInt32(5)
                        });
                    }
                }
            }

            return gates;
        }

        private static PersistedFm26SnapshotRecord ReadSnapshot(SqliteDataReader reader)
        {
            return new PersistedFm26SnapshotRecord
            {
                SnapshotId = ReadString(reader, 0),
                GeneratedAtUtc = ParseTimestamp(ReadString(reader, 1)),
                SnapshotStatus = ReadString(reader, 2),
                SafeMessage = ReadString(reader, 3),
                ConnectorAvailability = ReadString(reader, 4),
                PlatformStatus = ReadString(reader, 5),
                ProcessDetected = ReadBool(reader, 6),
                ProcessStatus = ReadString(reader, 7),
                ProcessName = ReadString(reader, 8),
                ProcessId = ReadNullableInt(reader, 9),
                ProductVersion = ReadString(reader, 10),
                FileVersion = ReadString(reader, 11),
                Architecture = ReadString(reader, 12),
                ReadOnlyAccessStatus = ReadString(reader, 13),
                MemoryMapRegistryStatus = ReadString(reader, 14),
                MapsFound = reader.GetInt32(15),
                ValidatedMaps = reader.GetInt32(16),
                TemplateMaps = reader.GetInt32(17),
                InvalidMaps = reader.GetInt32(18),
                SelectedMapId = ReadString(reader, 19),
                SelectedMapDisplayName = ReadString(reader, 20),
                SelectedMapBuild = ReadString(reader, 21),
                AllGatesPassed = ReadBool(reader, 22),
                BlockingGate = ReadString(reader, 23),
                LiveReadingAllowed = ReadBool(reader, 24),
                NextActionSafeMessage = ReadString(reader, 25),
                WarningCount = reader.GetInt32(26),
                ErrorCount = reader.GetInt32(27)
            };
        }

        private static DateTimeOffset ParseTimestamp(string value)
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : DateTimeOffset.MinValue;
        }

        private static string Normalize(string? value)
        {
            return SafeText(value).Trim();
        }

        private static string SafeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var sanitized = DiagnosticSanitizer.Sanitize(value).Trim();
            return sanitized.Length <= 1000 ? sanitized : sanitized.Substring(0, 1000);
        }

        private const string SnapshotSelectSql =
            @"SELECT snapshot_id, generated_at_utc, snapshot_status, safe_message,
                     connector_availability, platform_status, process_detected, process_status,
                     process_name, process_id, product_version, file_version, architecture,
                     read_only_access_status, memory_map_registry_status, maps_found, validated_maps,
                     template_maps, invalid_maps, selected_map_id, selected_map_display_name,
                     selected_map_build, all_gates_passed, blocking_gate, live_reading_allowed,
                     next_action_safe_message, warning_count, error_count
              FROM fm26_snapshot_runs";
    }
}
