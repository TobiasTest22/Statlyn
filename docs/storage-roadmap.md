# Storage Roadmap

SQLite remains the current storage implementation. It stores masked players, visible permitted fields, source metadata, workflow records, aggregate benchmark snapshots, safe audit counts and safe FM26 snapshot audit metadata.

PostgreSQL is a future option. The current migration direction is to keep repository and service boundaries in C# so a PostgreSQL implementation can be added later without letting frontend code access storage directly.

Rules:

- repositories stay in C#
- SQLite remains current
- PostgreSQL remains future
- React/Tauri never opens the database directly
- Unity never owns database business logic
- C++ never owns storage business logic
- raw provider data is not stored as frontend-facing data
- persisted FM26 snapshots are diagnostic metadata only and do not store player data, hidden values, memory addresses, raw offsets or native handles
