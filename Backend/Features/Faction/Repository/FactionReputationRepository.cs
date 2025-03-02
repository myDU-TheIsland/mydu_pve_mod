using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.Features.Faction.Data;
using Mod.DynamicEncounters.Features.Faction.Interfaces;
using NQ;

namespace Mod.DynamicEncounters.Features.Faction.Repository;

public class FactionReputationRepository(IServiceProvider provider) : IFactionReputationRepository
{
    private readonly IPostgresConnectionFactory _factory = provider.GetRequiredService<IPostgresConnectionFactory>();

    public async Task AddFactionReputationAsync(PlayerId playerId, FactionId factionId, long reputation)
    {
        using var connection = _factory.Create();
        connection.Open();

        var currentReputation = await GetFactionReputationAsync(playerId, factionId);
        if (currentReputation == null)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO mod_faction_rep (
                    reputation,
                    player_id,
                    faction_id,
                    updated_at
                ) 
                VALUES(
                    @reputation,
                    @player_id,
                    @faction_id,
                    NOW()
                )
                """,
                new
                {
                    player_id = (long)playerId.id,
                    faction_id = factionId.Id,
                    reputation
                });
            
            return;
        }

        await connection.ExecuteAsync(
            """
            UPDATE mod_faction_rep SET
                reputation = reputation + @reputation,
                updated_at = NOW()
            WHERE player_id = @player_id AND faction_id = @faction_id
            """,
            new
            {
                player_id = (long)playerId.id,
                faction_id = factionId.Id,
                reputation
            });
    }

    public async Task<long?> GetFactionReputationAsync(PlayerId playerId, FactionId factionId)
    {
        using var connection = _factory.Create();
        connection.Open();

        return await connection.ExecuteScalarAsync<long>(
            """
            SELECT reputation FROM mod_faction_rep WHERE player_id = @player_id AND faction_id = @faction_id
            """,
            new
            {
                player_id = (long)playerId.id,
                faction_id = factionId.Id
            }
        );
    }

    public async Task<IEnumerable<FactionReputationItem>> GetPlayerFactionReputationAsync(PlayerId playerId)
    {
        using var connection = _factory.Create();
        connection.Open();
        
        var rows = await connection.QueryAsync<DbRow>(
            """
            SELECT 
                F.id,
                FR.player_id,
                COALESCE(FR.reputation, 0) reputation,
                F.name faction_name
            FROM mod_faction F
            INNER JOIN mod_faction_rep FR ON F.id = FR.faction_id
            WHERE FR.player_id = @player_id
            """,
            new
            {
                player_id = (long)playerId.id,
            }
        );

        return rows.Select(MapDbRow);
    }
    
    private struct DbRow
    {
        public long reputation { get; set; }
        public ulong player_id { get; set; }
        public long faction_id { get; set; }
        public string faction_name { get; set; }
    }

    private FactionReputationItem MapDbRow(DbRow row)
    {
        return new FactionReputationItem
        {
            Reputation = row.reputation,
            FactionId = row.faction_id,
            FactionName = row.faction_name,
        };
    }
}