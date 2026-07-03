using System;
using System.IO;

namespace Statlyn.Data
{
    public enum StatlynDatabasePathMode
    {
        RuntimeMain = 0,
        RuntimeSmokeTest = 1,
        UnitTestInMemory = 2
    }

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

        public string ResolvePath(string? appDataRoot, StatlynDatabasePathMode mode)
        {
            if (mode == StatlynDatabasePathMode.UnitTestInMemory)
            {
                return ":memory:";
            }

            var root = string.IsNullOrWhiteSpace(appDataRoot)
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                : appDataRoot;

            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.GetTempPath();
            }

            if (mode == StatlynDatabasePathMode.RuntimeSmokeTest)
            {
                return Path.Combine(root, "StatlynSmokeTest", "statlyn-smoke-test.db");
            }

            return Path.Combine(root, "statlyn.db");
        }

        public string ResolveSmokeTestPath(string? temporaryRoot)
        {
            return ResolvePath(temporaryRoot, StatlynDatabasePathMode.RuntimeSmokeTest);
        }
    }
}
