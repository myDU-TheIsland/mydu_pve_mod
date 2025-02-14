using System;
using Mod.DynamicEncounters.Features.Scripts.Lua.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Lua.Services.Functions;
using MoonSharp.Interpreter;

namespace Mod.DynamicEncounters.Features.Scripts.Lua.Services;

public class LuaWorkflowPlayerScriptEnvironment(Action<string> printAction) : ILuaEnvironment
{
    public Script GetScript()
    {
        var script = new Script
        {
            Options =
            {
                DebugPrint = printAction
            }
        };

        var scriptNs = new Table(script)
        {
            ["getContext"] = ContextFunctions.GetContext(script)
        };

        script.Globals["script"] = scriptNs;

        return script;
    }
}