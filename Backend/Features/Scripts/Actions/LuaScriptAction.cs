using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Services;
using Mod.DynamicEncounters.Features.Scripts.Lua.Services;
using Mod.DynamicEncounters.Helpers;
using Temporalio.Activities;

namespace Mod.DynamicEncounters.Features.Scripts.Actions;

[ScriptActionName(ActionName)]
public class LuaScriptAction(ScriptActionItem actionItem) : IScriptAction
{
    private readonly ILogger _logger = ModBase.ServiceProvider.CreateLogger<LuaScriptAction>();
    
    public const string ActionName = "lua";
    
    public string Name => ActionName;
    public string GetKey() => Name;

    public async Task<ScriptActionResult> ExecuteAsync(ScriptContext context)
    {
        var env = new LuaWorkflowPlayerScriptEnvironment(s =>
        {
            _logger.LogInformation(s);
            if (ActivityExecutionContext.HasCurrent)
            {
                ActivityExecutionContext.Current.Logger.LogInformation(s);
            }
        });

        await LuaRunner.RunAsync(actionItem.Script, env);
        
        return ScriptActionResult.Successful();
    }
}