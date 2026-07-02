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

        public RoleModel RequireAttribute(string attributeName, double weight)
        {
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                throw new ArgumentException("Attribute name is required.", nameof(attributeName));
            }

            _attributeWeights[attributeName] = weight <= 0 ? 1 : weight;
            return this;
        }
    }
}
