using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.Features.Common.Data;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using NQ;

namespace Mod.DynamicEncounters.Features.Common.Services;

public class ConstructRepository(IServiceProvider provider) : IConstructRepository
{
    private readonly IPostgresConnectionFactory _factory = provider.GetRequiredService<IPostgresConnectionFactory>();
    
    public async Task<IEnumerable<ConstructItem>> FindByKind(ConstructKind kind)
    {
        using var db = _factory.Create();
        db.Open();

        var result = (await db.QueryAsync<DbRow>(
            $"""
            SELECT * FROM public.construct WHERE json_properties->>'kind' = '{(int)kind}'
            """
        )).ToList();

        return result.Select(MapToModel);
    }

    public async Task<IEnumerable<ConstructItem>> FindAsteroids()
    {
        using var db = _factory.Create();
        db.Open();

        var result = (await db.QueryAsync<DbRow>(
            """
             SELECT C.* FROM public.construct C
             INNER JOIN public.asteroid A ON A.construct_id = C.id
             WHERE json_properties->>'kind' = '2' AND deleted_at IS NULL
             """
        )).ToList();

        return result.Select(MapToModel);
    }

    private static ConstructItem MapToModel(DbRow row)
    {
        return new ConstructItem
        {
            Name = row.name,
            Position = new Vec3
            {
                x = row.position_x,
                y = row.position_y,
                z = row.position_z,
            }
        };
    }

    private struct DbRow
    {
        public string name { get; set; }
        public double position_x { get; set; }
        public double position_y { get; set; }
        public double position_z { get; set; }
    }
}