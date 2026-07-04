# NPM Audit Notes

Milestone 3.0 keeps the React/Tauri desktop dependency set stable. `npm audit` currently reports two development-tooling findings:

| Package | Severity | Dependency Type | Range | Recommended Fix | Decision |
| --- | --- | --- | --- | --- | --- |
| `vite` | High | Direct dev dependency | `<=6.4.2` | Upgrade to `vite@8.1.3` | Not applied because it is a semver-major upgrade. |
| `esbuild` | Moderate | Transitive dev dependency through Vite | `<=0.24.2` | Upgrade via `vite@8.1.3` | Not applied because the available fix is the same semver-major Vite upgrade. |

The findings affect the local frontend build/dev toolchain, not shipped C# decision logic, the API DTO contract or Tauri Rust runtime code. Do not run `npm audit fix --force` without a dedicated Vite/Tauri compatibility pass.

Current decision:

- keep Vite 5 for the Milestone 3.0 stabilization pass
- document the audit findings as known limitations
- revisit a Vite major upgrade in a later dependency-maintenance milestone
- verify `npm install`, `npm run build` and `npm run tauri:build` after any future upgrade
