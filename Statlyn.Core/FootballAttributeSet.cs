using System;
using System.Collections.Generic;

namespace Statlyn.Core
{
    public sealed class FootballAttributeSet
    {
        private readonly Dictionary<string, int?> _values;

        public FootballAttributeSet()
            : this(new Dictionary<string, int?>())
        {
        }

        public FootballAttributeSet(IDictionary<string, int?> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            _values = new Dictionary<string, int?>(values, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyDictionary<string, int?> Values
        {
            get { return _values; }
        }

        public int? Get(string name)
        {
            if (name == null)
            {
                return null;
            }

            return _values.TryGetValue(name, out var value) ? value : null;
        }
    }
}
