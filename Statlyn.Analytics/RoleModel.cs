using System;
using System.Collections.Generic;

namespace Statlyn.Analytics
{
    public sealed class RoleModel
    {
        private readonly Dictionary<string, double> _attributeWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        public RoleModel(string roleName)
        {
            RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName));
        }

        public string RoleName { get; }

        public IReadOnlyDictionary<string, double> AttributeWeights
        {
            get { return _attributeWeights; }
        }

        private readonly Dictionary<string, double> _statWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double> _physicalWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double> _scoutObservationWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _redFlags = new List<string>();
        private readonly List<string> _minimumRequirements = new List<string>();
        private readonly List<string> _confidenceRules = new List<string>();
        private readonly Dictionary<string, string> _evidenceTemplates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, double> StatWeights
        {
            get { return _statWeights; }
        }

        public IReadOnlyDictionary<string, double> PhysicalWeights
        {
            get { return _physicalWeights; }
        }

        public IReadOnlyDictionary<string, double> ScoutObservationWeights
        {
            get { return _scoutObservationWeights; }
        }

        public IReadOnlyList<string> RedFlags
        {
            get { return _redFlags; }
        }

        public IReadOnlyList<string> MinimumRequirements
        {
            get { return _minimumRequirements; }
        }

        public IReadOnlyList<string> ConfidenceRules
        {
            get { return _confidenceRules; }
        }

        public IReadOnlyDictionary<string, string> EvidenceTemplates
        {
            get { return _evidenceTemplates; }
        }

        public RoleModel RequireAttribute(string attributeName, double weight)
        {
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                throw new ArgumentException("Attribute name is required.", nameof(attributeName));
            }

            _attributeWeights[attributeName] = weight <= 0 ? 1 : weight;
            return this;
        }

        public RoleModel RequireStat(string statName, double weight)
        {
            AddWeighted(_statWeights, statName, weight);
            return this;
        }

        public RoleModel RequirePhysicalMetric(string metricName, double weight)
        {
            AddWeighted(_physicalWeights, metricName, weight);
            return this;
        }

        public RoleModel RequireScoutObservation(string observationName, double weight)
        {
            AddWeighted(_scoutObservationWeights, observationName, weight);
            return this;
        }

        public RoleModel AddRedFlag(string redFlag)
        {
            if (!string.IsNullOrWhiteSpace(redFlag))
            {
                _redFlags.Add(redFlag);
            }

            return this;
        }

        public RoleModel AddMinimumRequirement(string requirement)
        {
            if (!string.IsNullOrWhiteSpace(requirement))
            {
                _minimumRequirements.Add(requirement);
            }

            return this;
        }

        public RoleModel AddConfidenceRule(string rule)
        {
            if (!string.IsNullOrWhiteSpace(rule))
            {
                _confidenceRules.Add(rule);
            }

            return this;
        }

        public RoleModel AddEvidenceTemplate(string key, string template)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _evidenceTemplates[key] = template ?? string.Empty;
            }

            return this;
        }

        private static void AddWeighted(IDictionary<string, double> target, string name, double weight)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name is required.", nameof(name));
            }

            target[name] = weight <= 0 ? 1 : weight;
        }
    }
}
