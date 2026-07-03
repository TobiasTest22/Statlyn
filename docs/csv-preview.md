# CSV Preview

`CsvPreviewService` provides the read-only preview used by the Data Sources UI.

## Inputs

- Local CSV file path.
- Optional `FieldMappingSet`.
- `SourceMetadata`.

## Outputs

- File readability.
- Source name and file path.
- Row count.
- Detected columns.
- Column mapping previews.
- Safe, unknown, forbidden and permission-blocked counts.
- Safe diagnostics.

## Mapping Rules

Preview uses `FootballFieldCatalog` and optional explicit mappings. Forbidden raw names override explicit mappings, so a column named `CurrentAbility`, `PA` or `Professionalism` remains forbidden even if a mapping tries to treat it as a safe field.

Unknown fields are not considered usable. Licensed external metrics, images, provider flags and club badges can be marked permission-blocked when source metadata does not permit them.

## Storage Rules

Preview does not run import and does not store anything. It reads headers and row counts, not hidden cell values. Diagnostics and UI view models may show field names and counts, but not hidden raw values.

FM26 remains unsupported until validated memory maps exist. CSV preview is not a live FM26 integration.
