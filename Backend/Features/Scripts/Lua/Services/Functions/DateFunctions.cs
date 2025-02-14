using System;
using MoonSharp.Interpreter;

namespace Mod.DynamicEncounters.Features.Scripts.Lua.Services.Functions;

public static class DateFunctions
{
    public static Func<DynValue> Now() => () => DynValue.NewNumber(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
}