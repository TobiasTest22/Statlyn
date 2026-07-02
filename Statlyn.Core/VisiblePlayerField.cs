namespace Statlyn.Core
{
    public sealed class VisiblePlayerField
    {
        public VisiblePlayerField(
            PlayerFieldKey key,
            string fieldName,
            string sourceFieldName,
            string displayValue,
            double? numericValue,
            FieldValueKind valueKind,
            bool isKnown,
            bool isBlocked,
            bool canDisplay,
            bool canScore,
            bool canStore,
            int confidence,
            string sourceProvider,
            string missingReason)
        {
            Key = key;
            FieldName = fieldName ?? string.Empty;
            SourceFieldName = sourceFieldName ?? string.Empty;
            InstanceKey = new FieldInstanceKey(key, FieldName, SourceFieldName);
            DisplayValue = displayValue ?? string.Empty;
            NumericValue = numericValue;
            ValueKind = valueKind;
            IsKnown = isKnown;
            IsBlocked = isBlocked;
            CanDisplay = canDisplay;
            CanScore = canScore;
            CanStore = canStore;
            Confidence = Clamp(confidence);
            SourceProvider = sourceProvider ?? string.Empty;
            MissingReason = missingReason ?? string.Empty;
        }

        public PlayerFieldKey Key { get; }

        public FieldInstanceKey InstanceKey { get; }

        public string FieldName { get; }

        public string SourceFieldName { get; }

        public string DisplayValue { get; }

        public double? NumericValue { get; }

        public FieldValueKind ValueKind { get; }

        public bool IsKnown { get; }

        public bool IsBlocked { get; }

        public bool CanDisplay { get; }

        public bool CanScore { get; }

        public bool CanStore { get; }

        public int Confidence { get; }

        public string SourceProvider { get; }

        public string MissingReason { get; }

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
