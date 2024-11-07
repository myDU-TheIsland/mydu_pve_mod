using System;

namespace Mod.DynamicEncounters.Common.Helpers;

public static class EnvironmentVariableHelper
{
    public static string GetEnvironmentVarOrDefault(string varName, string defaultValue)
    {
        var envVar = Environment.GetEnvironmentVariable(varName);

        if (string.IsNullOrEmpty(envVar))
        {
            return defaultValue;
        }

        return envVar;
    }
}