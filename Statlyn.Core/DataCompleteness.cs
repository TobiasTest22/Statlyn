namespace Statlyn.Core
{
    public sealed class DataCompleteness
    {
        public DataCompleteness(int knownFields, int expectedFields)
        {
            KnownFields = knownFields < 0 ? 0 : knownFields;
            ExpectedFields = expectedFields < 0 ? 0 : expectedFields;
        }

        public int KnownFields { get; }

        public int ExpectedFields { get; }

        public int Percentage
        {
            get
            {
                if (ExpectedFields == 0)
                {
                    return 0;
                }

                return Clamp((int)System.Math.Round((double)KnownFields / ExpectedFields * 100.0));
            }
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
