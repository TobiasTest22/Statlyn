using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Statlyn.DataProviders.Fm26
{
    internal static class SafeConnectorText
    {
        private static readonly Regex HexAddressPattern = new Regex("0x[0-9A-Fa-f]{4,}", RegexOptions.Compiled);

        public static string Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return HexAddressPattern.Replace(value.Trim(), "[redacted]");
        }

        public static string FileNameLabel(string? value, string fallback)
        {
            var safe = Sanitize(value);
            if (string.IsNullOrWhiteSpace(safe))
            {
                return fallback;
            }

            try
            {
                var fileName = Path.GetFileName(safe);
                return string.IsNullOrWhiteSpace(fileName) ? fallback : fileName;
            }
            catch (ArgumentException)
            {
                return fallback;
            }
        }

        public static string DirectoryLabel(string? value)
        {
            var safe = Sanitize(value);
            if (string.IsNullOrWhiteSpace(safe))
            {
                return string.Empty;
            }

            try
            {
                var directory = Path.GetDirectoryName(safe);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    return string.Empty;
                }

                var label = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return string.IsNullOrWhiteSpace(label) ? "Detected FM folder" : label;
            }
            catch (ArgumentException)
            {
                return "Detected FM folder";
            }
        }

        public static string PathLabel(string? value, string fallback)
        {
            var fileName = FileNameLabel(value, fallback);
            var directory = DirectoryLabel(value);
            return string.IsNullOrWhiteSpace(directory) ? fileName : directory + Path.DirectorySeparatorChar + fileName;
        }
    }
}
