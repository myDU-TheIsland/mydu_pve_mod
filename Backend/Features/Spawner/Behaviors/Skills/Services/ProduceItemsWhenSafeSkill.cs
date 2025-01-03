using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class ProduceItemsWhenSafeSkill(ProduceItemsWhenSafeSkill.ProduceItemsWhenSafeSkillItem skillItem) : GiveTakeItemSkill(skillItem)
{
    public override async Task Use(BehaviorContext context)
    {
        var position = context.Position ?? context.Sector;
        var provider = context.Provider;
        var areaScanService = provider.GetRequiredService<IAreaScanService>();
        var contacts = (await areaScanService.ScanForNpcConstructs(position, skillItem.AreScanRange))
            .Select(x => x.ConstructId)
            .ToHashSet();

        contacts.Remove(context.ConstructId);
        
        if (contacts.Count != 0) return;

        var playerContacts = (await areaScanService.ScanForPlayerContacts(
            context.ConstructId,
            position,
            skillItem.AreScanRange)).ToList();

        if (!playerContacts.Any())
        {
            return;
        }

        var firstPlayerContact = playerContacts.First();

        OverrideConstructId = firstPlayerContact.ConstructId;
        
        await base.Use(context);
    }

    public new static ProduceItemsWhenSafeSkill Create(JToken jObj)
    {
        return new ProduceItemsWhenSafeSkill(jObj.ToObject<ProduceItemsWhenSafeSkillItem>());
    }
    
    public class ProduceItemsWhenSafeSkillItem : GiveTakeItemSkillItem
    {
        [JsonProperty] public double AreScanRange { get; set; } = DistanceHelpers.OneSuInMeters * 8D;
    }
}