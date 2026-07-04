using System.Runtime.InteropServices;

namespace Statlyn.DataProviders.Fm26
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NativeFm26ProcessInfo
    {
        public uint ProcessId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 520)]
        public string ExecutablePath;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string ProductVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Architecture;

        public int Detected;

        public int ReadOnlyAccess;
    }
}
