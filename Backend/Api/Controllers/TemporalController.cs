using System;
using Microsoft.AspNetCore.Mvc;

namespace Mod.DynamicEncounters.Api.Controllers;

[Route("temporal")]
public class TemporalController : Controller
{
    [HttpGet]
    [Route("config")]
    public IActionResult GetConfig()
    {
        return Ok(new
        {
            Host = Environment.GetEnvironmentVariable("TEMPORAL_HOST")
        });
    }
}