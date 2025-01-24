using System;
using Backend.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NQ;
using NQutils.Def;

namespace Mod.DynamicEncounters.Api.Controllers;

[Route("mining-unit")]
public class MiningUnitController : Controller
{
    [HttpPost]
    [Route("calibrate/{constructId:long}/element/{elementId}")]
    public IActionResult Calibrate(ulong constructId, ulong elementId)
    {
        var provider = ModBase.ServiceProvider;
        var dataAccessor = provider.GetRequiredService<IDataAccessor>();

        var date = DateTime.UtcNow.AddHours(-24);
        
        dataAccessor.SetDynamicProperty(constructId, elementId, MiningUnit.d_currentMiningRate, 1000);
        dataAccessor.SetDynamicProperty(constructId, elementId, MiningUnit.d_maxMiningRate, 1000);
        dataAccessor.SetDynamicProperty(constructId, elementId, MiningUnit.d_calibrationAtCalibrationTime, 1d);
        dataAccessor.SetDynamicProperty(constructId, elementId, MiningUnit.d_lastCalibrationTime, date.ToNQTimePoint().networkTime);
        dataAccessor.SetDynamicProperty(constructId, elementId, MiningUnit.d_activationTime, date.ToNQTimePoint().networkTime);
        dataAccessor.SetDynamicProperty(constructId, elementId, MiningUnit.d_lastPreviousCalibrationValue, 1d);
        dataAccessor.SetDynamicProperty(constructId, elementId, MiningUnit.d_Status, 1L);
        dataAccessor.SetDynamicProperty(constructId, elementId, MiningUnit.d_lastExtractionCalibrationBonus, 1D);
        
        return Ok();
    }
}