using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Services;
using Mod.DynamicEncounters.Helpers;
using NQ.Interfaces;

namespace Mod.DynamicEncounters.Features.Scripts.Actions;

[ScriptActionName(ActionName)]
public partial class TagSectorAsActiveScriptAction : IScriptAction
{
    public const string ActionName = "tag-sector-active";
    public string GetKey() => Name;

    public string Name => ActionName;

    public async Task<ScriptActionResult> ExecuteAsync(ScriptContext context)
    {
        var script = new CompositeScriptAction([
            new ForEachConstructHandleTaggedOnSectorAction(
                "poi",
                new DelayedScriptAction(
                    new ScriptActionItem
                    {
                        Actions = [
                            new ScriptActionItem
                            {
                                Type = "delete"
                            }
                        ]
                    }
                )
            )
        ]);

        context.Properties.TryAdd("DelaySeconds", TimeSpan.FromMinutes(30).TotalSeconds);

        if (context.ConstructId.HasValue)
        {
            var orleans = context.ServiceProvider.GetOrleans();
            var constructService = context.ServiceProvider.GetRequiredService<IConstructService>();
            var info = await constructService.GetConstructInfoAsync(context.ConstructId.Value);

            if (!info.ConstructExists)
            {
                return ScriptActionResult.Successful();
            }
            
            var name = ReplaceBetweenBracketsWithExclamation(info.Info!.rData.name);
            
            var cg = orleans.GetConstructGrain(context.ConstructId.Value);
            
            // TODO this may fail with faction ownership
            await cg.RenameConstruct(info.Info.mutableData.ownerId.playerId, name);
        }

        return await script.ExecuteAsync(context);
    }
    
    public static string ReplaceBetweenBracketsWithExclamation(string input)
    {
        return SquareBracketsContents().Replace(input, "[!!!]");
    }

    [GeneratedRegex(@"\[.*?\]")]
    private static partial Regex SquareBracketsContents();
}