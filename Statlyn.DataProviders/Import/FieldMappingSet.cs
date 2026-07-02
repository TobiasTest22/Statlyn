using System.Collections.Generic;
using System.Linq;
using Statlyn.Core;

namespace Statlyn.DataProviders.Import
{
    public sealed class FieldMappingSet
    {
        private readonly List<FieldMapping> _mappings;

        public FieldMappingSet(IEnumerable<FieldMapping> mappings)
        {
            _mappings = new List<FieldMapping>(mappings ?? Enumerable.Empty<FieldMapping>());
        }

        public IReadOnlyList<FieldMapping> Mappings
        {
            get { return _mappings; }
        }

        public FieldMapping Resolve(string columnName, FieldPolicyRegistry registry)
        {
            if (registry.IsForbiddenRawName(columnName))
            {
                return new FieldMapping(columnName, registry.ResolveKey(columnName, PlayerFieldKey.Unknown), columnName, FieldValueKind.Number);
            }

            foreach (var mapping in _mappings)
            {
                if (string.Equals(mapping.SourceColumn, columnName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return mapping;
                }
            }

            return new FootballFieldCatalog(registry).Resolve(columnName);
        }
    }
}
