﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Common.Data;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.Features.Sector.Data;
using Mod.DynamicEncounters.Features.Sector.Interfaces;
using Mod.DynamicEncounters.Helpers;
using Newtonsoft.Json;
using NQ;

namespace Mod.DynamicEncounters.Features.Sector.Repository;

public class SectorInstanceRepository(IServiceProvider provider) : ISectorInstanceRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory =
        provider.GetRequiredService<IPostgresConnectionFactory>();

    public async Task AddAsync(SectorInstance item)
    {
        using var db = _connectionFactory.Create();
        db.Open();

        await db.ExecuteAsync(
            """
            INSERT INTO public.mod_sector_instance (id, name, faction_id, sector_x, sector_y, sector_z, expires_at, on_load_script, on_sector_enter_script, force_expire_at, territory_id, json_properties)
            VALUES (@Id, @Name, @FactionId, @PosX, @PosY, @PosZ, @ExpiresAt, @OnLoadScript, @OnSectorEnterScript, @ForceExpiresAt, @TerritoryId, @json_properties::jsonb);
            """,
            new
            {
                item.Id,
                item.Name,
                item.FactionId,
                item.ForceExpiresAt,
                PosX = item.Sector.x,
                PosY = item.Sector.y,
                PosZ = item.Sector.z,
                item.ExpiresAt,
                item.OnLoadScript,
                item.OnSectorEnterScript,
                item.TerritoryId,
                json_properties = JsonConvert.SerializeObject(item.Properties)
            }
        );
    }

    public Task SetAsync(IEnumerable<SectorInstance> items)
    {
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SectorInstance item)
    {
        throw new NotImplementedException();
    }

    public Task AddRangeAsync(IEnumerable<SectorInstance> items)
    {
        return Task.CompletedTask;
    }

    public async Task<SectorInstance?> FindAsync(object key)
    {
        using var db = _connectionFactory.Create();
        db.Open();
        
        var result = (await db.QueryAsync<DbRow>("SELECT * FROM public.mod_sector_instance WHERE id = @id", (Guid)key)).ToList();

        if (result.Count > 0)
        {
            var first = result.First();

            return MapToModel(first);
        }

        return null;
    }

    private static SectorInstance MapToModel(DbRow first)
    {
        return new SectorInstance
        {
            Id = first.id,
            Name = first.name,
            FactionId = first.faction_id,
            ExpiresAt = first.expires_at,
            ForceExpiresAt = first.force_expire_at,
            OnLoadScript = first.on_load_script,
            OnSectorEnterScript = first.on_sector_enter_script,
            StartedAt = first.started_at,
            TerritoryId = first.territory_id,
            Properties = JsonConvert.DeserializeObject<SectorInstanceProperties>(first.json_properties),
            Sector = new Vec3
            {
                x = first.sector_x,
                y = first.sector_y,
                z = first.sector_z,
            }
        };
    }

    public async Task<IEnumerable<SectorInstance>> GetAllAsync()
    {
        using var db = _connectionFactory.Create();
        db.Open();

        var queryResult = await db.QueryAsync<DbRow>("SELECT * FROM public.mod_sector_instance ORDER BY created_at");

        return queryResult.Select(MapToModel);
    }

    public async Task<long> GetCountAsync()
    {
        using var db = _connectionFactory.Create();
        db.Open();

        return await db.ExecuteScalarAsync<long>("SELECT COUNT(0) FROM public.mod_sector_instance");
    }

    public async Task DeleteAsync(object key)
    {
        using var db = _connectionFactory.Create();
        db.Open();

        await db.ExecuteAsync(
            """
            DELETE FROM public.mod_sector_instance WHERE id = @id
            """,
            new
            {
                id = key
            }
        );
    }

    public Task Clear()
    {
        throw new NotImplementedException();
    }

    public async Task<SectorInstance?> FindById(Guid id)
    {
        using var db = _connectionFactory.Create();
        db.Open();

        var result = (await db.QueryAsync<DbRow>(
            """
            SELECT * FROM public.mod_sector_instance WHERE id = @id
            """,
            new
            {
                id
            }
        )).ToList();

        if (!result.Any())
        {
            return null;
        }

        return MapToModel(result[0]);
    }

    public async Task<SectorInstance?> FindBySector(Vec3 sector)
    {
        using var db = _connectionFactory.Create();
        db.Open();

        var result = (await db.QueryAsync<DbRow>(
            """
            SELECT * FROM public.mod_sector_instance WHERE sector_x = @x AND sector_y = @y AND sector_z = @z
            """,
            new
            {
                sector.x,
                sector.y,
                sector.z,
            }
        )).ToList();

        if (!result.Any())
        {
            return null;
        }

        return MapToModel(result[0]);
    }

    public async Task<IEnumerable<SectorInstance>> FindExpiredAsync()
    {
        using var db = _connectionFactory.Create();
        db.Open();

        var queryResult =
            await db.QueryAsync<DbRow>("SELECT * FROM public.mod_sector_instance WHERE expires_at < NOW() OR (force_expire_at IS NOT NULL AND force_expire_at < NOW())");

        return queryResult.Select(MapToModel);
    }

    public async Task DeleteExpiredAsync()
    {
        using var db = _connectionFactory.Create();
        db.Open();

        await db.ExecuteScalarAsync("DELETE FROM public.mod_sector_instance WHERE expires_at < NOW() OR (force_expire_at IS NOT NULL AND force_expire_at < NOW())");
    }

    public async Task<IEnumerable<SectorInstance>> ScanForInactiveSectorsVisitedByPlayersV2(double distance)
    {
        using var db = _connectionFactory.Create();
        db.Open();

        var queryResult = await db.QueryAsync<DbRow>(
            """
            SELECT * FROM (
            	SELECT 
            		SI.*,
            		(
            			SELECT C.id FROM construct C
            			LEFT JOIN mod_npc_construct_handle CH ON (CH.construct_id = C.id)
            			WHERE CH.id IS NULL AND
            				C.deleted_at IS NULL AND
            				(C.json_properties->>'isUntargetable' = 'false' OR C.json_properties->>'isUntargetable' IS NULL) AND
            				(C.json_properties->>'kind' IN ('4', '5')) AND
            				(C.owner_entity_id IS NOT NULL) AND
            				ST_DWithin(
            					ST_MakePoint(SI.sector_x, SI.sector_y, SI.sector_z),
            					C.position,
            					@distance
            				) AND
            				ST_3DDistance(C.position, ST_MakePoint(SI.sector_x, SI.sector_y, SI.sector_z)) <= @distance
            			LIMIT 1
            		) IS NOT NULL should_activate
            	FROM mod_sector_instance SI
            	WHERE SI.started_at IS NULL
            ) AS Q WHERE should_activate IS TRUE
            """,
            new { distance }
        );
        
        return queryResult.Select(MapToModel);
    }

    public async Task ExpireAllAsync()
    {
        using var db = _connectionFactory.Create();
        db.Open();
        
        await db.ExecuteAsync(
            """
             UPDATE public.mod_sector_instance SET expires_at = NOW()
             """
        );
    }
    
    public async Task ForceExpireAllAsync()
    {
        using var db = _connectionFactory.Create();
        db.Open();
        
        await db.ExecuteAsync(
            """
             UPDATE public.mod_sector_instance SET force_expire_at = NOW()
             """
        );
    }

    public async Task<long> GetCountWithTagAsync(string tag)
    {
        using var db = _connectionFactory.Create();
        db.Open();

        return await db.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(0) FROM public.mod_sector_instance AS SI
            INNER JOIN public.mod_faction AS F ON (F.id = SI.faction_id)
            WHERE F.tag = @tag
            """,
            new
            {
                tag
            }
        );
    }

    public async Task<long> GetCountByTerritoryAsync(Guid territoryId)
    {
        using var db = _connectionFactory.Create();
        db.Open();

        return await db.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(0) FROM public.mod_sector_instance AS SI
            WHERE SI.territory_id = @territoryId
            """,
            new
            {
                territoryId
            }
        );
    }

    public async Task ExpireSectorsWithDeletedConstructHandles()
    {
        using var db = _connectionFactory.Create();
        db.Open();

        await db.ExecuteAsync(
            """
            UPDATE public.mod_sector_instance
            	SET expires_at = NOW()
            WHERE id IN (
            	SELECT SI.id FROM public.mod_npc_construct_handle CH
            	INNER JOIN public.construct C ON (C.id = CH.construct_id)
            	INNER JOIN public.mod_sector_instance SI ON (SI.sector_x = CH.sector_x AND SI.sector_y = CH.sector_y AND SI.sector_z = CH.sector_z)
            	WHERE C.deleted_at IS NOT NULL AND CH.deleted_at IS NULL
            )            
            """
        );
    }

    public async Task SetExpirationFromNowAsync(Guid id, TimeSpan span)
    {
        using var db = _connectionFactory.Create();
        db.Open();

        var interval = span.ToPostgresInterval();
        
        await db.ExecuteAsync(
            $"""
             UPDATE public.mod_sector_instance SET expires_at = NOW() + INTERVAL '{interval}'
             WHERE id = @id
             """,
            new
            {
                id
            }
        );
    }

    public async Task<IEnumerable<SectorInstance>> FindUnloadedAsync()
    {
        using var db = _connectionFactory.Create();
        db.Open();

        var queryResult =
            await db.QueryAsync<DbRow>("SELECT * FROM public.mod_sector_instance WHERE loaded_at IS NULL AND (publish_at IS NULL OR publish_at <= NOW())");

        return queryResult.Select(MapToModel);
    }

    public async Task<IEnumerable<SectorInstance>> FindActiveAsync()
    {
        using var db = _connectionFactory.Create();
        db.Open();

        var queryResult =
            await db.QueryAsync<DbRow>("SELECT * FROM public.mod_sector_instance WHERE started_at IS NOT NULL");

        return queryResult.Select(MapToModel);
    }

    public async Task SetLoadedAsync(Guid id, bool loaded)
    {
        using var db = _connectionFactory.Create();
        db.Open();

        if (loaded)
        {
            await db.ExecuteAsync("UPDATE public.mod_sector_instance SET loaded_at = NOW() WHERE id = @id", new { id });
        }
        else
        {
            await db.ExecuteAsync("UPDATE public.mod_sector_instance SET loaded_at = NULL WHERE id = @id", new { id });
        }
    }

    public async Task TagAsStartedAsync(Guid id)
    {
        using var db = _connectionFactory.Create();
        db.Open();

        await db.ExecuteAsync("UPDATE public.mod_sector_instance SET started_at = NOW() WHERE id = @id", new { id });
    }

    public async Task<IEnumerable<SectorInstance>> FindSectorsRequiringStartupAsync()
    {
        using var db = _connectionFactory.Create();
        db.Open();

        var queryResult = await db.QueryAsync<DbRow>(
            $"""
            SELECT Si.* FROM public.construct C
            INNER JOIN public.mod_sector_instance SI ON (C.sector_x = SI.sector_x AND C.sector_y = SI.sector_y AND C.sector_z = SI.sector_z)
            LEFT JOIN public.ownership O ON (C.owner_entity_id = O.id)
            WHERE Si.started_at IS NULL AND 
                  C.owner_entity_id IS NOT NULL AND 
                  (SI.publish_at IS NULL OR SI.publish_at <= NOW()) AND
                  (O.player_id NOT IN({StaticPlayerId.Aphelia}, {StaticPlayerId.Unknown}) OR (O.player_id IS NULL AND O.organization_id IS NOT NULL))
            """
        );

        return queryResult.Select(MapToModel);
    }
    
    // TODO Fix query. It's not performant
    public async Task<IEnumerable<SectorInstance>> ScanForInactiveSectorsVisitedByPlayers(double distance)
    {
        using var db = _connectionFactory.Create();
        db.Open();

        var queryResult = await db.QueryAsync<DbRow>(
            """
            SELECT 
            	SI.* 
            FROM public.construct C
            LEFT JOIN mod_npc_construct_handle CH ON (CH.construct_id = C.id)
            INNER JOIN public.mod_sector_instance SI 
            	ON ST_DWithin(
            		ST_MakePoint(SI.sector_x, SI.sector_y, SI.sector_z),
            		C.position,
            		@distance
            	)
            WHERE SI.started_at IS NULL
            	AND CH.id IS NULL
            	AND C.deleted_at IS NULL
            	AND (C.json_properties->>'isUntargetable' = 'false' OR C.json_properties->>'isUntargetable' IS NULL)
            	AND (C.json_properties->>'kind' IN ('4', '5'))
            	AND (C.owner_entity_id IS NOT NULL)
            	AND ST_3DDistance(C.position, ST_MakePoint(SI.sector_x, SI.sector_y, SI.sector_z)) <= @distance
            """,
            new { distance }
        );
        
        return queryResult.Select(MapToModel);
    }

    private struct DbRow
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public long faction_id { get; set; }
        public double sector_x { get; set; }
        public double sector_y { get; set; }
        public double sector_z { get; set; }
        public string on_load_script { get; set; }
        public string on_sector_enter_script { get; set; }
        public DateTime expires_at { get; set; }
        public DateTime? force_expire_at { get; set; }
        public DateTime? started_at { get; set; }
        public Guid territory_id { get; set; }
        public string json_properties { get; set; }
    }
}