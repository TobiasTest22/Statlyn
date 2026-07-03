namespace Statlyn.Data
{
    public static class RuntimeDatabaseFactory
    {
        public static StatlynDbConnectionFactory CreateDefault(string? appDataRoot = null)
        {
            var path = new StatlynDatabasePathResolver().ResolveDefaultPath(appDataRoot);
            return CreateFile(path);
        }

        public static StatlynDbConnectionFactory CreateFile(string databasePath)
        {
            var factory = StatlynDbConnectionFactory.CreateFile(databasePath);
            new StatlynDatabaseInitializer(factory).Initialize();
            return factory;
        }

        public static StatlynDbConnectionFactory CreateForMode(string appDataRoot, StatlynDatabasePathMode mode)
        {
            if (mode == StatlynDatabasePathMode.UnitTestInMemory)
            {
                return CreateInMemory();
            }

            return CreateFile(new StatlynDatabasePathResolver().ResolvePath(appDataRoot, mode));
        }

        public static StatlynDbConnectionFactory CreateInMemory(string? name = null)
        {
            var factory = StatlynDbConnectionFactory.CreateInMemory(name);
            new StatlynDatabaseInitializer(factory).Initialize();
            return factory;
        }
    }
}
