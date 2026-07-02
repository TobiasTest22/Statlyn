# External Data Compliance

Statlyn must not scrape or bypass unlicensed football data sources.

Allowed future inputs include:

- Licensed APIs.
- Club-owned datasets.
- User-provided CSV or JSON exports.
- Manual scout reports.
- Official partner integrations.
- Other sources with clear permission for the intended use.

Every provider must tag:

- Source name.
- Provider type.
- Licence status.
- Allowed usage.
- Data completeness.
- Source confidence.
- Whether images may be displayed.
- Whether flags may be displayed.

If a source does not permit player faces, the player profile must use initials or a generic silhouette. If flags are not permitted and no bundled safe flag exists, the flag field is blocked.

No provider may claim live or licensed access without validation.
