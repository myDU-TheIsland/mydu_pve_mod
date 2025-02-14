using MoonSharp.Interpreter;

namespace Mod.DynamicEncounters.Features.Scripts.Lua.Interfaces;

public interface ILuaEnvironment
{
    Script GetScript();
}