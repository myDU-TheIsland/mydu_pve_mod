using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.Scripts.Lua.Interfaces;

namespace Mod.DynamicEncounters.Features.Scripts.Lua.Services;

public static class LuaRunner
{
    public static Task RunAsync(string luaCode, ILuaEnvironment environment)
    {
        environment.GetScript().DoString(luaCode);

        return Task.CompletedTask;
    }
}