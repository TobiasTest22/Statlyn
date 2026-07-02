using System;
using System.Collections.Generic;
using Statlyn.Core;

namespace Statlyn.DataProviders.Import
{
    public sealed class FootballFieldCatalog
    {
        private readonly Dictionary<string, FieldMapping> _mappings = new Dictionary<string, FieldMapping>(StringComparer.OrdinalIgnoreCase);
        private readonly FieldPolicyRegistry _registry;

        public FootballFieldCatalog(FieldPolicyRegistry registry)
        {
            _registry = registry;
            RegisterDefaults();
        }

        public FieldMapping Resolve(string sourceColumn)
        {
            var forbidden = _registry.ResolveKey(sourceColumn, PlayerFieldKey.Unknown);
            if (_registry.IsForbiddenRawName(sourceColumn))
            {
                return new FieldMapping(sourceColumn, forbidden, sourceColumn, FieldValueKind.Number);
            }

            return _mappings.TryGetValue(Normalize(sourceColumn), out var mapping)
                ? mapping
                : new FieldMapping(sourceColumn, PlayerFieldKey.Unknown, sourceColumn, FieldValueKind.Text);
        }

        private void RegisterDefaults()
        {
            Text("SourcePlayerId", PlayerFieldKey.SourcePlayerId, "SourcePlayerId");
            Text("DisplayName", PlayerFieldKey.DisplayName, "DisplayName");
            Text("Name", PlayerFieldKey.DisplayName, "DisplayName");
            Number("Age", PlayerFieldKey.Age, "Age");
            Text("Nationality", PlayerFieldKey.Nationality, "Nationality");
            Text("PrimaryPosition", PlayerFieldKey.PrimaryPosition, "PrimaryPosition");
            Text("Position", PlayerFieldKey.PrimaryPosition, "PrimaryPosition");
            Text("PreferredFoot", PlayerFieldKey.PreferredFoot, "PreferredFoot");
            Text("Club", PlayerFieldKey.Club, "Club");
            Text("League", PlayerFieldKey.League, "League");
            Text("Height", PlayerFieldKey.Height, "Height");

            foreach (var attribute in new[]
            {
                "Finishing", "Pace", "Acceleration", "FirstTouch", "Passing", "Technique", "Dribbling", "Crossing",
                "Tackling", "Marking", "Positioning", "OffTheBall", "WorkRate", "Stamina", "Strength", "Decisions",
                "Composure", "Vision", "Anticipation", "Balance", "Agility"
            })
            {
                Number(attribute, PlayerFieldKey.TechnicalAttribute, attribute);
            }

            foreach (var stat in new[]
            {
                "xG", "xA", "Goals", "Assists", "Shots", "ShotsOnTarget", "KeyPasses", "ProgressivePasses",
                "ProgressiveCarries", "Tackles", "Interceptions", "Blocks", "Clearances", "AerialDuelsWonPct",
                "GroundDuelsWonPct", "PassCompletionPct", "Minutes"
            })
            {
                Number(stat, PlayerFieldKey.PlayerStat, stat);
            }

            foreach (var metric in new[]
            {
                "TopSpeed", "SprintDistance", "HighSpeedRunning", "DistanceCovered", "Accelerations", "Decelerations"
            })
            {
                Number(metric, PlayerFieldKey.PhysicalData, metric);
            }

            Text("ScoutTechnical", PlayerFieldKey.ScoutObservation, "Technical");
            Text("ScoutTactical", PlayerFieldKey.ScoutObservation, "Tactical");
            Text("ScoutPhysical", PlayerFieldKey.ScoutObservation, "Physical");
            Text("ScoutMental", PlayerFieldKey.ScoutObservation, "Mental");
            Number("PressingEffort", PlayerFieldKey.ScoutObservation, "PressingEffort");
            Text("BodyLanguage", PlayerFieldKey.ScoutObservation, "BodyLanguage");
            Text("Coachability", PlayerFieldKey.ScoutObservation, "Coachability");
        }

        private void Text(string sourceColumn, PlayerFieldKey key, string fieldName)
        {
            Add(sourceColumn, key, fieldName, FieldValueKind.Text);
        }

        private void Number(string sourceColumn, PlayerFieldKey key, string fieldName)
        {
            Add(sourceColumn, key, fieldName, FieldValueKind.Number);
        }

        private void Add(string sourceColumn, PlayerFieldKey key, string fieldName, FieldValueKind valueKind)
        {
            _mappings[Normalize(sourceColumn)] = new FieldMapping(sourceColumn, key, fieldName, valueKind);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var characters = new List<char>();
            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    characters.Add(char.ToLowerInvariant(character));
                }
            }

            return new string(characters.ToArray());
        }
    }
}
