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
    }
}
