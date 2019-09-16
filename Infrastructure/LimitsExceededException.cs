using System;

namespace trurl
{
    public class LimitsExceededException : Exception
    {
        private readonly string limit;
        private readonly int max;

        public LimitsExceededException(string limit, int max)
        {
            this.limit = limit;
            this.max = max;
        }

        public string GetMessage(string command)
        {
            return string.Format("exceeded permitted {0} (max {1})", limit, max);
        }
    }
}
