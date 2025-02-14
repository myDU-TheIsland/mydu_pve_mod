using System;
using MoonSharp.Interpreter;

namespace Mod.DynamicEncounters.Features.Scripts.Lua.Services.Functions;

public static class ContextFunctions
{
    public static Func<DynValue> GetContext(Script script)
        => () =>
        {
            var table = new Table(script)
            {
                ["now"] = DateFunctions.Now()
            };

            return DynValue.NewTable(table);
        };
}