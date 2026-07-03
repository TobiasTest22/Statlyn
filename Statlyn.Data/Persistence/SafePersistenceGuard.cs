using System;
using Statlyn.Core.Abstractions;

namespace Statlyn.Data.Persistence
{
    internal static class SafePersistenceGuard
    {
        public static void RejectRaw(object value, string operation)
        {
            if (value is IRawFootballEntity)
            {
                throw new InvalidOperationException(operation + " cannot accept raw provider data.");
            }
        }
    }
}
