using System;
using System.Collections.Generic;
using System.Linq;
using Statlyn.DataProviders.Fm26;

namespace Statlyn.DataProviders.Fm26.MemoryMaps
{
    public sealed class MemoryMapRegistryValidator
    {
        private static readonly HashSet<string> VisibleValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "visible",
            "alwaysVisible",
            "public",
            "display"
        };

        private static readonly HashSet<string> HiddenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hidden",
            "neverVisible",
            "blocked",
            "forbidden"
        };

        private static readonly HashSet<string> UnknownValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "unknown",
            "unknownUntilValidated",
            "unmapped"
        };

        private static readonly string[] SensitiveFieldTokens =
        {
            "currentability",
            "potentialability",
            "professionalism",
            "hidden",
            "personality"
        };

        public MemoryMapValidationResult Validate(MemoryMapManifest manifest)
        {
            if (manifest == null)
            {
                return Invalid("Map metadata could not be read.");
            }

            var warnings = new List<string>();
            var errors = new List<string>();

            var target = manifest.BuildTarget ?? new MemoryMapBuildTarget();
            manifest.BuildTarget = target;
            var game = target.Game;
            if (string.IsNullOrWhiteSpace(game) || game.IndexOf("Football Manager 26", StringComparison.OrdinalIgnoreCase) < 0 && game.IndexOf("FM26", StringComparison.OrdinalIgnoreCase) < 0)
            {
                errors.Add("Map game metadata must target FM26.");
            }

            if (string.IsNullOrWhiteSpace(manifest.MapId))
            {
                errors.Add("Map id is required.");
            }

            if (string.IsNullOrWhiteSpace(manifest.DisplayName))
            {
                errors.Add("Display name is required.");
            }

            if (!manifest.IsTemplate)
            {
                if (string.IsNullOrWhiteSpace(target.GameVersion))
                {
                    errors.Add("Game version is required for non-template maps.");
                }

                if (string.IsNullOrWhiteSpace(target.BuildNumber))
                {
                    errors.Add("Build number is required for non-template maps.");
                }

                if (string.IsNullOrWhiteSpace(target.Architecture))
                {
                    errors.Add("Architecture is required for non-template maps.");
                }
            }

            if (ContainsWriteUsage(manifest.AllowedUsage))
            {
                errors.Add("Write-enabled map usage is rejected.");
            }

            if (!manifest.IsValidated)
            {
                warnings.Add(manifest.IsTemplate ? "Template maps are not usable." : "Map is not validated.");
            }

            ValidateFields(manifest.Fields, errors, warnings);

            if (errors.Count > 0)
            {
                return new MemoryMapValidationResult
                {
                    IsValid = false,
                    IsUsable = false,
                    SupportStatus = MemoryMapSupportStatus.Invalid.ToString(),
                    SafeMessage = "Map metadata failed guardrail validation.",
                    Warnings = warnings,
                    Errors = errors
                };
            }

            if (manifest.IsTemplate)
            {
                return new MemoryMapValidationResult
                {
                    IsValid = true,
                    IsUsable = false,
                    SupportStatus = MemoryMapSupportStatus.TemplateOnly.ToString(),
                    SafeMessage = "Template map metadata loaded. Templates are not usable.",
                    Warnings = warnings.Count == 0 ? new[] { "Template maps are not usable." } : warnings,
                    Errors = Array.Empty<string>()
                };
            }

            if (!manifest.IsValidated)
            {
                return new MemoryMapValidationResult
                {
                    IsValid = true,
                    IsUsable = false,
                    SupportStatus = MemoryMapSupportStatus.Unvalidated.ToString(),
                    SafeMessage = "Map metadata loaded but is not validated.",
                    Warnings = warnings.Count == 0 ? new[] { "Map is not validated." } : warnings,
                    Errors = Array.Empty<string>()
                };
            }

            return new MemoryMapValidationResult
            {
                IsValid = true,
                IsUsable = true,
                SupportStatus = MemoryMapSupportStatus.MapAvailable.ToString(),
                SafeMessage = "Validated map metadata is available. Player reading is not implemented.",
                Warnings = warnings,
                Errors = Array.Empty<string>()
            };
        }

        private static MemoryMapValidationResult Invalid(string message)
        {
            return new MemoryMapValidationResult
            {
                IsValid = false,
                IsUsable = false,
                SupportStatus = MemoryMapSupportStatus.Invalid.ToString(),
                SafeMessage = SafeConnectorText.Sanitize(message),
                Warnings = Array.Empty<string>(),
                Errors = new[] { SafeConnectorText.Sanitize(message) }
            };
        }

        private static void ValidateFields(IReadOnlyList<MemoryMapFieldDefinition> fields, List<string> errors, List<string> warnings)
        {
            for (var index = 0; index < fields.Count; index++)
            {
                var field = fields[index];
                var visibility = NormalizeVisibility(field);
                var isHidden = visibility == "hidden" || IsSensitiveField(field);

                field.IsHidden = isHidden;
                field.Visibility = visibility;

                if (string.IsNullOrWhiteSpace(field.FieldKey) && string.IsNullOrWhiteSpace(field.FieldName))
                {
                    errors.Add("Every field must have a safe key or name.");
                }

                if (string.IsNullOrWhiteSpace(field.Category))
                {
                    errors.Add("Every field must declare a category.");
                }

                if (string.IsNullOrWhiteSpace(field.DataType))
                {
                    errors.Add("Every field must declare a data type.");
                }

                if (visibility == "unknown")
                {
                    errors.Add("Unknown field visibility is denied by default.");
                }

                if (!field.IsReadOnly)
                {
                    errors.Add("Write-enabled field access is rejected.");
                }

                if (isHidden && (field.CanDisplay || field.CanStore || field.CanScore))
                {
                    errors.Add("Blocked hidden fields cannot display, store or score.");
                }

                if (isHidden && !field.CanDisplay && !field.CanStore && !field.CanScore)
                {
                    warnings.Add("A hidden field is blocked from display, storage and scoring.");
                }
            }
        }

        private static string NormalizeVisibility(MemoryMapFieldDefinition field)
        {
            var value = string.IsNullOrWhiteSpace(field.Visibility) ? string.Empty : field.Visibility.Trim();
            if (VisibleValues.Contains(value))
            {
                return "visible";
            }

            if (HiddenValues.Contains(value))
            {
                return "hidden";
            }

            if (UnknownValues.Contains(value) || string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            return "unknown";
        }

        private static bool IsSensitiveField(MemoryMapFieldDefinition field)
        {
            var text = ((field.FieldKey ?? string.Empty) + " " + (field.FieldName ?? string.Empty)).Replace("_", string.Empty).Replace("-", string.Empty);
            return SensitiveFieldTokens.Any(token => text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                || string.Equals(field.FieldKey, "CA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.FieldName, "CA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.FieldKey, "PA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.FieldName, "PA", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsWriteUsage(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && (value.IndexOf("write", StringComparison.OrdinalIgnoreCase) >= 0
                    || value.IndexOf("modify", StringComparison.OrdinalIgnoreCase) >= 0
                    || value.IndexOf("inject", StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
