# Native Connector Boundary

`Statlyn.NativeConnector` is connector-only. Its job is future FM data access preparation:

- FM process detection
- read-only connector preparation
- memory map loading
- raw snapshot extraction
- connector diagnostics
- native build validation

It must not contain:

- recruitment logic
- role fit logic
- benchmark logic
- scouting firewall logic
- UI logic
- database business logic

The connector remains read-only. Statlyn does not write to FM process memory and does not fake FM26 support when no validated memory map exists.
