using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Statlyn.DataProviders.Fm26;

namespace Statlyn.DataProviders.Fm26.MemoryMaps
{
    public sealed class MemoryMapRegistryLoader
    {
        private readonly string _memoryMapsDirectory;
        private readonly MemoryMapRegistryValidator _validator;

        public MemoryMapRegistryLoader(string memoryMapsDirectory)
            : this(memoryMapsDirectory, new MemoryMapRegistryValidator())
        {
        }

        public MemoryMapRegistryLoader(string memoryMapsDirectory, MemoryMapRegistryValidator validator)
        {
            _memoryMapsDirectory = memoryMapsDirectory ?? string.Empty;
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public static MemoryMapRegistryLoader FromAppBase(string appBaseDirectory)
        {
            return new MemoryMapRegistryLoader(ResolveMemoryMapsDirectory(appBaseDirectory));
        }

        public MemoryMapRegistryDiagnostic Load()
        {
            if (string.IsNullOrWhiteSpace(_memoryMapsDirectory) || !Directory.Exists(_memoryMapsDirectory))
            {
                return new MemoryMapRegistryDiagnostic
                {
                    RegistryStatus = MemoryMapSupportStatus.RegistryMissing.ToString(),
                    MapsFoundCount = 0,
                    SafeMessage = "Memory-map registry directory was not found.",
                    NextActionSafeMessage = "Add validated FM26 map metadata before future player snapshot milestones."
                };
            }

            var diagnostics = new List<MemoryMapFileDiagnostic>();
            foreach (var file in Directory.GetFiles(_memoryMapsDirectory, "*.map.json", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                diagnostics.Add(LoadFile(file));
            }

            return BuildRegistry(diagnostics);
        }

        private MemoryMapFileDiagnostic LoadFile(string file)
        {
            var fileName = SafeConnectorText.FileNameLabel(file, "map.json");
            try
            {
                var json = File.ReadAllText(file);
                using (var document = JsonDocument.Parse(json))
                {
                    var manifest = ParseManifest(document.RootElement, fileName);
                    manifest.Checksum = ComputeChecksum(file);
                    var validation = _validator.Validate(manifest);
                    return ToFileDiagnostic(fileName, manifest, validation);
                }
            }
            catch (JsonException)
            {
                return InvalidFile(fileName, "Map JSON is malformed.");
            }
            catch (IOException)
            {
                return InvalidFile(fileName, "Map file could not be read.");
            }
            catch (UnauthorizedAccessException)
            {
                return InvalidFile(fileName, "Map file access was denied.");
            }
        }

        private static MemoryMapRegistryDiagnostic BuildRegistry(IReadOnlyList<MemoryMapFileDiagnostic> maps)
        {
            var invalid = maps.Count(item => item.SupportStatus == MemoryMapSupportStatus.Invalid.ToString());
            var templates = maps.Count(item => item.IsTemplate);
            var usable = maps.Count(item => item.IsUsable);
            var validated = maps.Any(item => item.IsValidated && item.SupportStatus != MemoryMapSupportStatus.Invalid.ToString());

            string status;
            string safeMessage;
            if (maps.Count == 0)
            {
                status = MemoryMapSupportStatus.Empty.ToString();
                safeMessage = "No FM26 map metadata files were found.";
            }
            else if (invalid > 0)
            {
                status = MemoryMapSupportStatus.Invalid.ToString();
                safeMessage = "One or more FM26 map metadata files failed validation.";
            }
            else if (usable > 0)
            {
                status = MemoryMapSupportStatus.MapAvailable.ToString();
                safeMessage = "Validated FM26 map metadata is available. Player reading is not implemented.";
            }
            else if (templates > 0)
            {
                status = MemoryMapSupportStatus.TemplateOnly.ToString();
                safeMessage = "Only FM26 map templates are available. Templates are not usable.";
            }
            else
            {
                status = MemoryMapSupportStatus.Unvalidated.ToString();
                safeMessage = "FM26 map metadata exists but no validated usable map is available.";
            }

            return new MemoryMapRegistryDiagnostic
            {
                RegistryStatus = status,
                MapsFoundCount = maps.Count,
                UsableMapsCount = usable,
                TemplateMapsCount = templates,
                InvalidMapsCount = invalid,
                HasValidatedMap = validated,
                SafeMessage = safeMessage,
                NextActionSafeMessage = usable > 0
                    ? "Validated map metadata is available; player reading remains disabled until a future safe snapshot milestone."
                    : "Validate an FM26 map before any future player snapshot milestone.",
                Maps = maps
            };
        }

        private static MemoryMapFileDiagnostic ToFileDiagnostic(string fileName, MemoryMapManifest manifest, MemoryMapValidationResult validation)
        {
            return new MemoryMapFileDiagnostic
            {
                FileName = fileName,
                MapId = manifest.MapId,
                DisplayName = manifest.DisplayName,
                GameVersion = manifest.BuildTarget.GameVersion,
                BuildNumber = manifest.BuildTarget.BuildNumber,
                Platform = manifest.BuildTarget.Platform,
                Architecture = manifest.BuildTarget.Architecture,
                IsTemplate = manifest.IsTemplate,
                IsValidated = manifest.IsValidated,
                IsUsable = validation.IsUsable,
                SupportStatus = validation.SupportStatus,
                FieldCount = manifest.FieldCount,
                VisibleFieldCount = manifest.VisibleFieldCount,
                HiddenFieldCountBlocked = manifest.HiddenFieldCountBlocked,
                Checksum = manifest.Checksum,
                SafeMessage = validation.SafeMessage,
                ValidationWarnings = validation.Warnings,
                ValidationErrors = validation.Errors,
                Manifest = manifest
            };
        }

        private static MemoryMapFileDiagnostic InvalidFile(string fileName, string message)
        {
            var safeMessage = SafeConnectorText.Sanitize(message);
            return new MemoryMapFileDiagnostic
            {
                FileName = fileName,
                SupportStatus = MemoryMapSupportStatus.Invalid.ToString(),
                IsUsable = false,
                SafeMessage = safeMessage,
                ValidationErrors = new[] { safeMessage }
            };
        }

        private static MemoryMapManifest ParseManifest(JsonElement root, string fileName)
        {
            var entity = ReadString(root, "entity");
            var build = ReadString(root, "build");
            var mapId = FirstNonEmpty(ReadString(root, "mapId"), entity, Path.GetFileNameWithoutExtension(fileName));
            var displayName = FirstNonEmpty(ReadString(root, "displayName"), entity.Length == 0 ? mapId : "FM26 " + entity + " map");
            var game = FirstNonEmpty(ReadString(root, "game"), "Football Manager 26");
            var buildTarget = ReadBuildTarget(root, game, build);

            return new MemoryMapManifest
            {
                MapId = SafeConnectorText.Sanitize(mapId),
                DisplayName = SafeConnectorText.Sanitize(displayName),
                BuildTarget = buildTarget,
                CreatedAtUtc = ReadDate(root, "createdAtUtc"),
                UpdatedAtUtc = ReadDate(root, "updatedAtUtc"),
                Source = SafeConnectorText.Sanitize(ReadString(root, "source")),
                IsTemplate = ReadBool(root, "isTemplate") || string.Equals(build, "template", StringComparison.OrdinalIgnoreCase),
                IsValidated = ReadBool(root, "isValidated") || ReadBool(root, "validated") || ReadBool(root, "supported"),
                ValidationNotes = SafeConnectorText.Sanitize(ReadString(root, "validationNotes")),
                AllowedUsage = FirstNonEmpty(ReadString(root, "allowedUsage"), "metadataOnly"),
                MinimumConnectorVersion = SafeConnectorText.Sanitize(ReadString(root, "minimumConnectorVersion")),
                Fields = ReadFields(root),
                SafeMessage = "FM26 map metadata loaded."
            };
        }

        private static MemoryMapBuildTarget ReadBuildTarget(JsonElement root, string game, string legacyBuild)
        {
            var target = TryGet(root, "buildTarget", out var buildTarget) && buildTarget.ValueKind == JsonValueKind.Object
                ? buildTarget
                : root;

            return new MemoryMapBuildTarget
            {
                Game = SafeConnectorText.Sanitize(FirstNonEmpty(ReadString(target, "game"), game)),
                GameVersion = SafeConnectorText.Sanitize(FirstNonEmpty(ReadString(target, "gameVersion"), ReadString(root, "gameVersion"), legacyBuild == "template" ? string.Empty : legacyBuild)),
                BuildNumber = SafeConnectorText.Sanitize(FirstNonEmpty(ReadString(target, "buildNumber"), ReadString(target, "build"), ReadString(root, "buildNumber"), legacyBuild == "template" ? string.Empty : legacyBuild)),
                Platform = SafeConnectorText.Sanitize(FirstNonEmpty(ReadString(target, "platform"), ReadString(root, "platform"), "Windows")),
                Architecture = SafeConnectorText.Sanitize(FirstNonEmpty(ReadString(target, "architecture"), ReadString(root, "architecture"), "x64"))
            };
        }

        private static IReadOnlyList<MemoryMapFieldDefinition> ReadFields(JsonElement root)
        {
            if (!TryGet(root, "fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<MemoryMapFieldDefinition>();
            }

            var list = new List<MemoryMapFieldDefinition>();
            foreach (var field in fields.EnumerateArray())
            {
                if (field.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = FirstNonEmpty(ReadString(field, "fieldName"), ReadString(field, "name"));
                var key = FirstNonEmpty(ReadString(field, "fieldKey"), name);
                list.Add(new MemoryMapFieldDefinition
                {
                    FieldKey = SafeConnectorText.Sanitize(key),
                    FieldName = SafeConnectorText.Sanitize(name),
                    Category = SafeConnectorText.Sanitize(FirstNonEmpty(ReadString(field, "category"), ReadString(field, "visibilityCategory"), "metadata")),
                    Visibility = SafeConnectorText.Sanitize(FirstNonEmpty(ReadString(field, "visibility"), ReadString(field, "visibilityCategory"), "unknown")),
                    DataType = SafeConnectorText.Sanitize(ReadString(field, "dataType")),
                    SymbolicReference = SafeConnectorText.Sanitize(FirstNonEmpty(ReadString(field, "symbolicReference"), ReadString(field, "pointerPath"))),
                    IsReadOnly = !TryGet(field, "isReadOnly", out var readOnlyElement) || readOnlyElement.ValueKind != JsonValueKind.False,
                    IsHidden = ReadBool(field, "isHidden"),
                    CanDisplay = ReadBool(field, "canDisplay"),
                    CanStore = ReadBool(field, "canStore"),
                    CanScore = ReadBool(field, "canScore"),
                    Notes = SafeConnectorText.Sanitize(ReadString(field, "notes"))
                });
            }

            return list;
        }

        private static string ComputeChecksum(string file)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(File.ReadAllBytes(file));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                {
                    builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        private static string ResolveMemoryMapsDirectory(string appBaseDirectory)
        {
            var directory = new DirectoryInfo(string.IsNullOrWhiteSpace(appBaseDirectory) ? AppContext.BaseDirectory : appBaseDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "memory-maps");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return Path.Combine(appBaseDirectory ?? string.Empty, "memory-maps");
        }

        private static bool TryGet(JsonElement element, string propertyName, out JsonElement value)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        private static string ReadString(JsonElement element, string propertyName)
        {
            if (!TryGet(element, propertyName, out var value))
            {
                return string.Empty;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return SafeConnectorText.Sanitize(value.GetString());
            }

            if (value.ValueKind == JsonValueKind.Number || value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            {
                return SafeConnectorText.Sanitize(value.ToString());
            }

            return string.Empty;
        }

        private static bool ReadBool(JsonElement element, string propertyName)
        {
            if (!TryGet(element, propertyName, out var value))
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return bool.TryParse(value.GetString(), out var parsed) && parsed;
            }

            return false;
        }

        private static DateTimeOffset? ReadDate(JsonElement element, string propertyName)
        {
            var value = ReadString(element, propertyName);
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }
    }
}
