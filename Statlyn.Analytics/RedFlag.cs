namespace Statlyn.Analytics
{
    public sealed class RedFlag
    {
        public RedFlag(string fieldName, RedFlagOperator operatorKind, double threshold, string message, string appliesToGroup)
        {
            FieldName = fieldName ?? string.Empty;
            OperatorKind = operatorKind;
            Threshold = threshold;
            Message = message ?? string.Empty;
            AppliesToGroup = appliesToGroup ?? string.Empty;
        }

        public string FieldName { get; }

        public RedFlagOperator OperatorKind { get; }

        public double Threshold { get; }

        public string Message { get; }

        public string AppliesToGroup { get; }
    }
}
