using System.Collections.Generic;

namespace Statlyn.DataProviders
{
    public sealed class DataCompletenessReport
    {
        public DataCompletenessReport(int knownFields, int expectedFields, IReadOnlyList<string> missingFields)
        {
            KnownFields = knownFields < 0 ? 0 : knownFields;
            ExpectedFields = expectedFields < 0 ? 0 : expectedFields;
            MissingFields = missingFields ?? new List<string>();
        }

        public int KnownFields { get; }

        public int ExpectedFields { get; }

        public IReadOnlyList<string> MissingFields { get; }

        public int CompletenessPercentage
        {
            get
            {
                if (ExpectedFields == 0)
                {
                    return 0;
                }

                return System.Math.Max(0, System.Math.Min(100, (int)System.Math.Round((double)KnownFields / ExpectedFields * 100.0)));
            }
        }
    }
}
