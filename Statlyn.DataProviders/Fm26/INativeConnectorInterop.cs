using System.Text;

namespace Statlyn.DataProviders.Fm26
{
    internal interface INativeConnectorInterop
    {
        int GetVersion(StringBuilder buffer, int bufferLength);

        int GetBuildInfo(StringBuilder buffer, int bufferLength);

        int DetectFmProcess(out NativeFm26ProcessInfo processInfo);

        int GetLastError(StringBuilder buffer, int bufferLength);

        void ResetLastError();
    }
}
