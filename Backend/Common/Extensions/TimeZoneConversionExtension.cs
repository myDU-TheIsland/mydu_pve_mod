using System;
using System.Collections.Generic;
using System.Linq;

namespace Mod.DynamicEncounters.Common.Extensions;

public static class TimeZoneConversionExtension
{
    public static DateTime ToUniversalTimeWithSourceTimeZone(this DateTime dateTime, TimeZoneInfo sourceTimeZone)
    {
        var result = dateTime;
        if (sourceTimeZone.IsInvalidTime(dateTime))
        {
            var adjustmentRules = sourceTimeZone.GetAdjustmentRules();
            result = adjustmentRules.ApplyAdjustmentRules(result);
        }

        return TimeZoneInfo.ConvertTimeToUtc(result, sourceTimeZone);
    }

    public static DateTime ApplyAdjustmentRules(this IEnumerable<TimeZoneInfo.AdjustmentRule> adjustmentRules, DateTime dateTime)
    {
        var validRules = adjustmentRules
            .Where(x => dateTime > x.DateStart && dateTime <= x.DateEnd)
            .ToList();

        if (!validRules.Any())
        {
            return dateTime;
        }

        var result = dateTime;
        foreach (var adjustmentRule in validRules)
        {
            result += adjustmentRule.DaylightDelta;
        }

        return result;
    }
}