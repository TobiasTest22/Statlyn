using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Statlyn.Tests
{
    public sealed class Milestone401Tests
    {
        [Fact]
        public void PackageJsonExposesDesktopPackagingModes()
        {
            var root = FindRepositoryRoot();
            var packagePath = Path.Combine(root, "Statlyn.Desktop", "package.json");
            using var document = JsonDocument.Parse(File.ReadAllText(packagePath));
            var scripts = document.RootElement.GetProperty("scripts");

            Assert.Contains("CARGO_BUILD_JOBS", scripts.GetProperty("tauri:build:lowmem").GetString(), StringComparison.Ordinal);
            Assert.Contains("CARGO_INCREMENTAL", scripts.GetProperty("tauri:build:lowmem").GetString(), StringComparison.Ordinal);
            Assert.Contains("--no-bundle", scripts.GetProperty("tauri:build:nobundle").GetString(), StringComparison.Ordinal);
            Assert.Contains("run-desktop-build-diagnostics.ps1", scripts.GetProperty("desktop:validate").GetString(), StringComparison.Ordinal);
            Assert.Contains("-SkipTauriBuild", scripts.GetProperty("desktop:validate:quick").GetString(), StringComparison.Ordinal);
            Assert.Contains("-LowMemory", scripts.GetProperty("desktop:validate:lowmem").GetString(), StringComparison.Ordinal);
        }

        [Fact]
        public void DesktopBuildDiagnosticsScriptClassifiesPackagingFailures()
        {
            var root = FindRepositoryRoot();
            var script = File.ReadAllText(Path.Combine(root, "tools", "run-desktop-build-diagnostics.ps1"));

            foreach (var required in new[]
            {
                "out-of-memory failure",
                "Rust compile failure",
                "installer bundling failure",
                "missing Rust toolchain",
                "missing WebView2/Windows dependency",
                "frontend failure",
                "CARGO_BUILD_JOBS",
                "CARGO_INCREMENTAL",
                "--no-bundle"
            })
            {
                Assert.Contains(required, script, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void TauriRustRemainsDisplayShellOnly()
        {
            var root = FindRepositoryRoot();
            var tauriRoot = Path.Combine(root, "Statlyn.Desktop", "src-tauri");
            var sourceText = string.Join(
                "\n",
                Directory.GetFiles(Path.Combine(tauriRoot, "src"), "*.rs", SearchOption.AllDirectories).Select(File.ReadAllText)
                    .Concat(new[]
                    {
                        File.ReadAllText(Path.Combine(tauriRoot, "Cargo.toml")),
                        File.ReadAllText(Path.Combine(tauriRoot, "tauri.conf.json"))
                    }));

            foreach (var forbidden in new[]
            {
                "sqlite",
                "rusqlite",
                "OpenProcess",
                "ReadProcessMemory",
                "NativeFm26Connector",
                "StatlynNativeConnector",
                "CurrentAbility",
                "PotentialAbility",
                "processMemory"
            })
            {
                Assert.DoesNotContain(forbidden, sourceText, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void DocsDescribeLowMemoryPackagingAndCiDesktopCoverage()
        {
            var root = FindRepositoryRoot();
            var docs = string.Join(
                "\n",
                File.ReadAllText(Path.Combine(root, "README.md")),
                File.ReadAllText(Path.Combine(root, "docs", "react-tauri-ui.md")),
                File.ReadAllText(Path.Combine(root, "docs", "testing.md")));

            Assert.Contains("low-memory", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("npm run tauri:build:lowmem", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("rustc/LLVM", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("desktop frontend build", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("run-desktop-build-diagnostics.ps1", docs, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("full installer", docs, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CiIncludesLightweightDesktopFrontendBuild()
        {
            var root = FindRepositoryRoot();
            var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));

            Assert.Contains("Desktop frontend build", workflow, StringComparison.Ordinal);
            Assert.Contains("actions/setup-node", workflow, StringComparison.Ordinal);
            Assert.Contains("npm ci", workflow, StringComparison.Ordinal);
            Assert.Contains("npm run check", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("tauri:build", workflow, StringComparison.OrdinalIgnoreCase);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Statlyn.sln")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new InvalidOperationException("Could not find repository root.");
            }

            return directory.FullName;
        }
    }
}
