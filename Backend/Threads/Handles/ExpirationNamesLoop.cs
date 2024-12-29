﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Sector.Interfaces;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters.Threads.Handles;

public class ExpirationNamesLoop(IThreadManager tm, CancellationToken ct) :
    ThreadHandle(ThreadId.ExpirationNames, tm, ct)
{
    public override async Task Tick()
    {
        var logger = ModBase.ServiceProvider.CreateLogger<ExpirationNamesLoop>();

        try
        {
            var sectorPoolManager = ModBase.ServiceProvider.GetRequiredService<ISectorPoolManager>();
            await sectorPoolManager.UpdateExpirationNames();

            ReportHeartbeat();
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to UpdateExpirationNames");

            Thread.Sleep(TimeSpan.FromSeconds(30));
        }
    }
}