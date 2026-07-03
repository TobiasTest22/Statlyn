using System;

namespace Statlyn.UI
{
    public enum ThemeMode
    {
        LightGlass,
        DarkCommandCenter
    }

    public enum CommandStatusCategory
    {
        Muted,
        Info,
        Accent,
        Success,
        Warning,
        Danger,
        Neutral
    }

    public sealed class ThemeToken
    {
        public ThemeToken(
            ThemeMode mode,
            string name,
            string background,
            string panel,
            string elevatedPanel,
            string border,
            string primaryAccent,
            string secondaryAccent,
            string success,
            string warning,
            string danger,
            string mutedText,
            string mainText,
            string subtleText)
        {
            Mode = mode;
            Name = name ?? string.Empty;
            Background = background ?? string.Empty;
            Panel = panel ?? string.Empty;
            ElevatedPanel = elevatedPanel ?? string.Empty;
            Border = border ?? string.Empty;
            PrimaryAccent = primaryAccent ?? string.Empty;
            SecondaryAccent = secondaryAccent ?? string.Empty;
            Success = success ?? string.Empty;
            Warning = warning ?? string.Empty;
            Danger = danger ?? string.Empty;
            MutedText = mutedText ?? string.Empty;
            MainText = mainText ?? string.Empty;
            SubtleText = subtleText ?? string.Empty;
        }

        public ThemeMode Mode { get; }

        public string Name { get; }

        public string Background { get; }

        public string Panel { get; }

        public string ElevatedPanel { get; }

        public string Border { get; }

        public string PrimaryAccent { get; }

        public string SecondaryAccent { get; }

        public string Success { get; }

        public string Warning { get; }

        public string Danger { get; }

        public string MutedText { get; }

        public string MainText { get; }

        public string SubtleText { get; }
    }

    public static class ThemeTokens
    {
        public static readonly ThemeToken LightGlass = new ThemeToken(
            ThemeMode.LightGlass,
            "Light glass",
            "#F4F7F9",
            "#FFFFFF",
            "#FAFCFE",
            "#BCC7D1",
            "#2D7188",
            "#318E76",
            "#197049",
            "#996312",
            "#A64242",
            "#5F6C78",
            "#1E272F",
            "#63717E");

        public static readonly ThemeToken DarkCommandCenter = new ThemeToken(
            ThemeMode.DarkCommandCenter,
            "Dark command center",
            "#060E16",
            "#0A1723",
            "#102231",
            "#244052",
            "#21D5E6",
            "#38CFA6",
            "#44D17A",
            "#E6B74A",
            "#F26C6C",
            "#8FA6B5",
            "#E3EEF5",
            "#B4C4D0");

        public static ThemeToken For(ThemeMode mode)
        {
            return mode == ThemeMode.LightGlass ? LightGlass : DarkCommandCenter;
        }

        public static string SafeModeLabel(ThemeMode mode)
        {
            return mode == ThemeMode.LightGlass ? "Light glass legacy fallback" : "Dark command center";
        }

        public static CommandStatusCategory ResolveStatusCategory(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return CommandStatusCategory.Muted;
            }

            var value = label.Trim();
            if (Contains(value, "failed") || Contains(value, "error") || Contains(value, "rejected"))
            {
                return CommandStatusCategory.Danger;
            }

            if (Contains(value, "available benchmark") || Contains(value, "smoke test passed"))
            {
                return CommandStatusCategory.Success;
            }

            if (Contains(value, "warning") || Contains(value, "unsupported") || Contains(value, "not checked") || Contains(value, "missing") || Contains(value, "pending") || Contains(value, "awaiting") || Contains(value, "insufficient") || Contains(value, "no benchmark"))
            {
                if (Contains(value, "no benchmark"))
                {
                    return CommandStatusCategory.Muted;
                }

                return CommandStatusCategory.Warning;
            }

            if (Contains(value, "not built yet"))
            {
                return CommandStatusCategory.Muted;
            }

            if (Contains(value, "no live fm26") || Contains(value, "csv-only") || Contains(value, "csv only") || Contains(value, "safe local") || Contains(value, "generic/import") || Contains(value, "local"))
            {
                return CommandStatusCategory.Info;
            }

            if (Contains(value, "passed") || Contains(value, "ok") || Contains(value, "complete") || Contains(value, "ready") || Contains(value, "active"))
            {
                return CommandStatusCategory.Success;
            }

            return CommandStatusCategory.Muted;
        }

        public static string StatusClassFor(CommandStatusCategory category)
        {
            switch (category)
            {
                case CommandStatusCategory.Info:
                    return "status-info";
                case CommandStatusCategory.Accent:
                    return "status-accent";
                case CommandStatusCategory.Success:
                    return "status-success";
                case CommandStatusCategory.Warning:
                    return "status-warning";
                case CommandStatusCategory.Danger:
                    return "status-danger";
                case CommandStatusCategory.Muted:
                case CommandStatusCategory.Neutral:
                    return "status-muted";
                default:
                    return "status-muted";
            }
        }

        public static CommandStatusCategory BenchmarkStatus(string safeMessage)
        {
            if (Contains(safeMessage ?? string.Empty, "available"))
            {
                return CommandStatusCategory.Success;
            }

            if (Contains(safeMessage ?? string.Empty, "insufficient"))
            {
                return CommandStatusCategory.Warning;
            }

            if (Contains(safeMessage ?? string.Empty, "no benchmark"))
            {
                return CommandStatusCategory.Muted;
            }

            return ResolveStatusCategory(safeMessage ?? string.Empty);
        }

        public static CommandStatusCategory Fm26Status(bool isSupported)
        {
            return isSupported ? CommandStatusCategory.Success : CommandStatusCategory.Warning;
        }

        public static string GlobalSafetyLabel(bool hasLiveFm26Data)
        {
            return hasLiveFm26Data ? "Live FM26 data unavailable" : "No live FM26 data";
        }

        public static string EmptyStateMessage(string subject)
        {
            var safeSubject = string.IsNullOrWhiteSpace(subject) ? "This page" : subject.Trim();
            return safeSubject + " is not built yet. No fake data is shown.";
        }

        public static string ErrorStateMessage(string subject)
        {
            var safeSubject = string.IsNullOrWhiteSpace(subject) ? "This page" : subject.Trim();
            return safeSubject + " could not load safely. No raw provider data is shown.";
        }

        private static bool Contains(string value, string expected)
        {
            return value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
