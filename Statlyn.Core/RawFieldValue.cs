using System;

namespace Statlyn.Core
{
    public sealed class RawFieldValue
    {
        public RawFieldValue(
            PlayerFieldKey key,
            string rawName,
            object? value,
            FieldValueKind valueKind,
            int confidence,
            bool isKnown = true,
            bool isEstimate = false)
        {
            Key = key;
            RawName = rawName ?? string.Empty;
            Value = value;
            ValueKind = valueKind;
            Confidence = Clamp(confidence);
            IsKnown = isKnown;
            IsEstimate = isEstimate;
        }

        public PlayerFieldKey Key { get; }

        public string RawName { get; }

        public object? Value { get; }

        public FieldValueKind ValueKind { get; }

        public int Confidence { get; }

        public bool IsKnown { get; }

        public bool IsEstimate { get; }

        public string DisplayValue
        {
            get { return Value == null ? string.Empty : Convert.ToString(Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty; }
        }

        public double? NumericValue
        {
            get
            {
                if (Value == null)
                {
                    return null;
                }

                if (Value is int intValue)
                {
                    return intValue;
                }

                if (Value is double doubleValue)
                {
                    return doubleValue;
                }

                if (Value is decimal decimalValue)
                {
                    return (double)decimalValue;
                }

                if (double.TryParse(DisplayValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    return parsed;
                }

                return null;
            }
        }

        public RawFieldValue WithKey(PlayerFieldKey key)
        {
            return new RawFieldValue(key, RawName, Value, ValueKind, Confidence, IsKnown, IsEstimate);
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
