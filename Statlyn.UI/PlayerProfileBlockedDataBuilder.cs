using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statlyn.Data.Profile;

namespace Statlyn.UI
{
    public static class PlayerProfileBlockedDataBuilder
    {
        public static PlayerProfileBlockedDataViewModel Build(PlayerProfileResult result)
        {
            var categories = result.BlockedFields.Select(field => field.Key.ToString()).Distinct().OrderBy(value => value).ToList();
            var reasons = result.BlockedFields.Select(field => field.Reason).Where(reason => !string.IsNullOrWhiteSpace(reason)).Distinct().OrderBy(value => value).ToList();
            var message = result.BlockedFields.Count == 0
                ? "No blocked fields were present."
                : result.BlockedFields.Count.ToString(CultureInfo.InvariantCulture) + " blocked field(s) excluded. Raw values are not shown.";
            return new PlayerProfileBlockedDataViewModel(result.BlockedFields.Count, categories, reasons, message);
        }
    }
}
