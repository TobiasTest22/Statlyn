using Statlyn.Core.Diagnostics;

namespace Statlyn.DataProviders.Fm26
{
    public interface IFm26NativeConnector
    {
        bool IsAvailable { get; }

        string LastError { get; }

        string GetConnectorVersion();

        string GetBuildInfo();

        Fm26ProcessDiagnostic DetectFmProcess();

        Fm26ConnectorDiagnostic GetDiagnostic();

        DiagnosticStatus ValidateBuild(Fm26ProcessDiagnostic processInfo);
    }
}
