using System.Collections.Generic;
using Statlyn.Core;
using Statlyn.Core.Diagnostics;
using Statlyn.DataProviders.Fm26;

namespace Statlyn.Tests
{
    internal sealed class UnsupportedBuildConnector : IFm26NativeConnector
    {
        public string ConnectorVersion
        {
            get { return "test-unsupported-connector"; }
        }

        public SnapshotResult<Fm26ProcessInfo> Detect()
        {
            var diagnostics = new DiagnosticReport();
            diagnostics.Add("fm26.process", DiagnosticStatus.Verified, "FM26 detected.", "Test connector reports a synthetic process but no data.");
            return SnapshotResult<Fm26ProcessInfo>.FromSuccess(new Fm26ProcessInfo
            {
                ProcessId = 4242,
                ExecutablePath = "C:\\Games\\Football Manager 26\\fm.exe",
                Architecture = "x64",
                ProductVersion = "26.0.0-test",
                ModuleBaseAddress = "0x00000000",
                IsDetected = true,
                HasReadOnlyAccess = true
            }, diagnostics);
        }

        public DiagnosticStatus ValidateBuild(Fm26ProcessInfo processInfo)
        {
            return DiagnosticStatus.Unsupported;
        }

        public SnapshotResult<IReadOnlyList<PlayerRawSnapshot>> ReadPlayerSnapshot()
        {
            var diagnostics = new DiagnosticReport();
            diagnostics.Add("fm26.snapshot.players", DiagnosticStatus.Failed, "This method should not be called for unsupported builds.", string.Empty);
            return SnapshotResult<IReadOnlyList<PlayerRawSnapshot>>.FromFailure("Unsupported", diagnostics);
        }
    }
}
