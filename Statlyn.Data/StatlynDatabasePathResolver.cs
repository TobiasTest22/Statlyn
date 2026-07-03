using System;
using System.IO;

namespace Statlyn.Data
{
    public sealed class StatlynDatabasePathResolver
    {
        public string ResolveDefaultPath()
        {
            return ResolveDefaultPath(null);
        }

        public string ResolveDefaultPath(string? appDataRoot)
        {
            var root = string.IsNullOrWhiteSpace(appDataRoot)
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                : appDataRoot;

            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.GetTempPath();
            }

            return Path.Combine(root, "Statlyn", "statlyn.db");
        }
    }
}
