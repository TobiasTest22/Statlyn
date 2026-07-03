# Database Maintenance

Milestone 2.8 adds a small local database maintenance foundation for release-candidate demos.

## Supported Actions

- Get local database status.
- Create a timestamped backup copy of the main SQLite database.
- Reset the smoke-test database.
- Clear the smoke-test database.
- Explicitly clear the main runtime database through an intentionally named method.

The normal Unity Diagnostics UI exposes backup and smoke-test reset only. It does not expose an accidental main-database wipe.

## Backup

`LocalDatabaseMaintenanceService.CreateTimestampedBackupCopy` copies the local SQLite file when it exists. If the file is missing, it returns a safe message and does not create fake data.

Backups are file copies only. The service does not inspect raw database contents.

## Smoke-Test Reset

Smoke-test reset uses `RuntimeSmokeTest` path resolution and reinitializes that database. It does not touch the main runtime database.

## Main Database Safety

The explicit main clear method is named `ExplicitlyClearMainRuntimeDatabase`. It exists for future controlled tooling, not for normal demo flow. Smoke tests and normal Diagnostics actions must not call it automatically.
