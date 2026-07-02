using System;

namespace Statlyn.Core
{
    public sealed class VisibleField<T>
    {
        private VisibleField(
            string fieldName,
            T? value,
            bool isKnown,
            bool canDisplay,
            bool canScore,
            int confidence,
            FieldVisibilityCategory category,
            string sourceProvider,
            string missingReason)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            Value = value;
            IsKnown = isKnown;
            CanDisplay = canDisplay;
            CanScore = canScore;
            Confidence = Clamp(confidence);
            Category = category;
            SourceProvider = sourceProvider ?? string.Empty;
            MissingReason = missingReason ?? string.Empty;
        }

        public string FieldName { get; }

        public T? Value { get; }

        public bool IsKnown { get; }

        public bool CanDisplay { get; }

        public bool CanScore { get; }

        public int Confidence { get; }

        public FieldVisibilityCategory Category { get; }

        public string SourceProvider { get; }

        public string MissingReason { get; }

        public static VisibleField<T> Known(string fieldName, T value, bool canScore, int confidence, FieldVisibilityCategory category, string sourceProvider)
        {
            return new VisibleField<T>(fieldName, value, true, true, canScore, confidence, category, sourceProvider, string.Empty);
        }

        public static VisibleField<T> Unknown(string fieldName, FieldVisibilityCategory category, string sourceProvider, string missingReason)
        {
            return new VisibleField<T>(fieldName, default(T), false, false, false, 0, category, sourceProvider, missingReason);
        }

        private static int Clamp(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > 100)
            {
                return 100;
            }

            return value;
        }
    }
}
