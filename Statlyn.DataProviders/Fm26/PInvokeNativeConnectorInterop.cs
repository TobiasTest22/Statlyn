using System.Runtime.InteropServices;
using System.Text;

namespace Statlyn.DataProviders.Fm26
{
    internal sealed class PInvokeNativeConnectorInterop : INativeConnectorInterop
    {
        private const string LibraryName = "Statlyn.NativeConnector";

        public int GetVersion(StringBuilder buffer, int bufferLength)
        {
            return StatlynConnector_GetVersion(buffer, bufferLength);
        }

        public int GetBuildInfo(StringBuilder buffer, int bufferLength)
        {
            return StatlynConnector_GetBuildInfo(buffer, bufferLength);
        }

        public int DetectFmProcess(out NativeFm26ProcessInfo processInfo)
        {
            return StatlynConnector_DetectFmProcess(out processInfo);
        }

        public int GetLastError(StringBuilder buffer, int bufferLength)
        {
            return StatlynConnector_GetLastError(buffer, bufferLength);
        }

        public void ResetLastError()
        {
            StatlynConnector_ResetLastError();
        }

        [DllImport(LibraryName, EntryPoint = "StatlynConnector_GetVersion", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int StatlynConnector_GetVersion(StringBuilder buffer, int bufferLength);

        [DllImport(LibraryName, EntryPoint = "StatlynConnector_GetBuildInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int StatlynConnector_GetBuildInfo(StringBuilder buffer, int bufferLength);

        [DllImport(LibraryName, EntryPoint = "StatlynConnector_DetectFmProcess", CallingConvention = CallingConvention.Cdecl)]
        private static extern int StatlynConnector_DetectFmProcess(out NativeFm26ProcessInfo processInfo);

        [DllImport(LibraryName, EntryPoint = "StatlynConnector_GetLastError", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int StatlynConnector_GetLastError(StringBuilder buffer, int bufferLength);

        [DllImport(LibraryName, EntryPoint = "StatlynConnector_ResetLastError", CallingConvention = CallingConvention.Cdecl)]
        private static extern void StatlynConnector_ResetLastError();
    }
}
