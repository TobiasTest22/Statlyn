using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statlyn.Core;
using Statlyn.Data.Profile;

namespace Statlyn.UI
{
    public static class PlayerProfileAttributeSupportBuilder
    {
        public static IReadOnlyList<PlayerProfileAttributeSupportViewModel> Build(PlayerProfileResult result)
        {
            return result.VisibleFields
                .Where(field => field.Key == PlayerFieldKey.TechnicalAttribute && field.CanDisplay && field.NumericValue.HasValue)
                .Take(8)
                .Select(field => new PlayerProfileAttributeSupportViewModel(field.FieldName, FormatNumber(field.NumericValue!.Value), field.Confidence.ToString(CultureInfo.InvariantCulture) + "%", "Supporting evidence only"))
                .ToList();
        }

        private static string FormatNumber(double value)
        {
            return Math.Abs(value - Math.Round(value)) < 0.001
                ? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
