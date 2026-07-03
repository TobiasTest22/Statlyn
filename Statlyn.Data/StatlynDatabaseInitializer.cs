using System;
using Statlyn.Core.Diagnostics;

namespace Statlyn.Data
{
    public sealed class StatlynDatabaseInitializer
    {
        private readonly StatlynMigrationRunner _migrationRunner;
        private readonly StatlynDbConnectionFactory _connectionFactory;

        public StatlynDatabaseInitializer(StatlynDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _migrationRunner = new StatlynMigrationRunner(connectionFactory);
        }

        public DiagnosticReport Initialize()
        {
            var diagnostics = new DiagnosticReport();
            _migrationRunner.ApplyMigrations();
            diagnostics.Add("database.initialized", DiagnosticStatus.Verified, "SQLite database initialized.", _connectionFactory.DatabasePath);
            diagnostics.Add("database.schema", DiagnosticStatus.Verified, "Schema version is current.", StatlynSchemaVersion.Current.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return diagnostics;
        }
    }
}
