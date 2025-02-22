﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.Features.Quests.Data;
using Mod.DynamicEncounters.Features.Quests.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Newtonsoft.Json;
using NQ;

namespace Mod.DynamicEncounters.Features.Quests.Repository;

public partial class PlayerQuestRepository(IServiceProvider provider) : IPlayerQuestRepository
{
    private readonly IPostgresConnectionFactory _factory = provider.GetRequiredService<IPostgresConnectionFactory>();

    public async Task AddAsync(PlayerQuestItem item)
    {
        using var db = _factory.Create();
        db.Open();

        using var transaction = db.BeginTransaction();
        
        try
        {
            await db.ExecuteAsync(
                """
                INSERT INTO public.mod_player_quest (
                    id, original_quest_id, player_id, faction_id, type, status, seed, json_properties, created_at, expires_at, on_success_script, on_failure_script
                ) VALUES (
                    @id, @original_quest_id, @player_id, @faction_id, @type, @status, @seed, @json_properties::jsonb, NOW(), @expires_at, @on_success_script::jsonb, @on_failure_script::jsonb              
                )
                """,
                new
                {
                    id = item.Id,
                    original_quest_id = item.OriginalQuestId,
                    player_id = (long)item.PlayerId,
                    faction_id = item.FactionId.Id,
                    type = item.Type,
                    status = item.Status,
                    seed = item.Seed,
                    json_properties = JsonConvert.SerializeObject(item.Properties),
                    expires_at = item.ExpiresAt,
                    on_success_script = JsonConvert.SerializeObject(item.OnSuccessScript),
                    on_failure_script = JsonConvert.SerializeObject(item.OnFailureScript),
                },
                transaction: transaction
            );

            foreach (var taskItem in item.TaskItems)
            {
                await db.ExecuteAsync(
                    """
                    INSERT INTO public.mod_player_quest_task (id, quest_id, text, type, status, position_x, position_y, position_z, json_properties, completed_at, base_construct_id, on_check_completed_script) 
                    VALUES (@id, @quest_id, @text, @type, @status, @position_x, @position_y, @position_z, @json_properties::jsonb, @completed_at, @base_construct_id, @on_check_completed_script::jsonb) 
                    """,
                    new
                    {
                        id = taskItem.Id.Id,
                        quest_id = item.Id,
                        type = taskItem.Type,
                        text = taskItem.Text,
                        status = taskItem.Status,
                        position_x = taskItem.Position.x,
                        position_y = taskItem.Position.y,
                        position_z = taskItem.Position.z,
                        json_properties = JsonConvert.SerializeObject(taskItem.Definition),
                        completed_at = (DateTime?)null,
                        base_construct_id = (long?)taskItem.BaseConstruct,
                        on_check_completed_script = JsonConvert.SerializeObject(taskItem.OnCheckScript)
                    },
                    transaction: transaction
                );
            }

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();

            throw;
        }
    }

    public Task UpdateAsync(PlayerQuestItem item)
    {
        throw new NotImplementedException();
    }

    public async Task<PlayerQuestItem?> GetAsync(QuestId id)
    {
        using var db = _factory.Create();
        db.Open();

        var result = (await db.QueryAsync<DbRow>(
            """
            SELECT 
                PQ.*,
                QT.id task_id,
                QT.base_construct_id task_base_construct_id,
                QT.completed_at task_completed_at,
                QT.json_properties task_json_properties,
                QT.on_check_completed_script task_on_check_completed_script,
                QT.position_x task_position_x,
                QT.position_y task_position_y,
                QT.position_z task_position_z,
                QT.status task_status,
                QT.text task_text,
                QT.type task_type
            FROM public.mod_player_quest PQ
            INNER JOIN public.mod_player_quest_task QT ON (QT.quest_id = PQ.id)
            WHERE PQ.deleted_at IS NULL AND PQ.id = @id
            """,
            new
            {
                id = id.Id
            }
        )).ToList();

        if (result.Count == 0)
        {
            return null;
        }
        
        var row = result[0];
        
        var item = MapToModel(row);
        item.TaskItems.Add(MapToTaskItem(row));

        return item;
    }

    public async Task<bool> OriginalQuestAlreadyAccepted(Guid proceduralQuestId)
    {
        using var db = _factory.Create();
        db.Open();

        var count = await db.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(0) FROM public.mod_player_quest_task PQT
            WHERE quest_id = @questId AND completed_at IS NULL
            """,
            new
            {
                questId = proceduralQuestId,
            }
        );

        return count > 0;
    }

    public async Task<IEnumerable<PlayerQuestItem>> GetAllByStatusAsync(PlayerId playerId, string[] statusList)
    {
        using var db = _factory.Create();
        db.Open();

        statusList = statusList
            .Select(s => SanitizeNonAlphaNumerics().Replace(s, ""))
            .Select(s => $"'{s}'")
            .ToArray();
        
        var statusListPredicate = string.Join(",", statusList); 

        var result = (await db.QueryAsync<DbRow>(
            $"""
            SELECT 
                PQ.*,
                QT.id task_id,
                QT.base_construct_id task_base_construct_id,
                QT.completed_at task_completed_at,
                QT.json_properties task_json_properties,
                QT.on_check_completed_script task_on_check_completed_script,
                QT.position_x task_position_x,
                QT.position_y task_position_y,
                QT.position_z task_position_z,
                QT.status task_status,
                QT.text task_text,
                QT.type task_type
            FROM public.mod_player_quest PQ
            INNER JOIN public.mod_player_quest_task QT ON (QT.quest_id = PQ.id)
            WHERE PQ.deleted_at IS NULL AND PQ.player_id = @playerId AND PQ.status IN ({statusListPredicate})
            """,
            new
            {
                playerId = (long)playerId.id
            }
        )).ToList();

        var map = new Dictionary<Guid, PlayerQuestItem>();

        foreach (var row in result)
        {
            map.TryAdd(row.id, MapToModel(row));
            map[row.id].TaskItems.Add(MapToTaskItem(row));
        }

        return map.Values;
    }

    public async Task DeleteAsync(PlayerId playerId, Guid id)
    {
        using var db = _factory.Create();
        db.Open();

        await db.ExecuteAsync(
            """
            UPDATE public.mod_player_quest SET deleted_at = NOW() WHERE id = @id AND player_id = @playerId
            """,
            new { id, playerId = (long)playerId.id }
        );
    }

    public async Task CompleteTaskAsync(QuestTaskId questTaskId)
    {
        using var db = _factory.Create();
        db.Open();

        await db.ExecuteAsync(
            """
            UPDATE public.mod_player_quest_task SET 
                completed_at = NOW(),
                status = @status
            WHERE id = @questTaskId AND quest_id = @questId
            """,
            new
            {
                questId = questTaskId.QuestId.Id,
                questTaskId = questTaskId.Id,
                status = QuestTaskItemStatus.Completed
            }
        );
    }

    public async Task SetStatusAsync(QuestId questId, string status)
    {
        using var db = _factory.Create();
        db.Open();

        await db.ExecuteAsync(
            """
            UPDATE public.mod_player_quest SET status = @status WHERE id = @questId
            """,
            new
            {
                questId = questId.Id,
                status
            }
        );
    }

    public async Task<bool> AreAllTasksCompleted(QuestId questId)
    {
        using var db = _factory.Create();
        db.Open();

        var count = await db.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(0) FROM public.mod_player_quest_task PQT
            WHERE quest_id = @questId AND completed_at IS NULL
            """,
            new
            {
                questId = questId.Id,
            }
        );

        return count == 0;
    }

    public async Task<dynamic> GetReport()
    {
        using var db = _factory.Create();
        db.Open();

        return await db.QueryAsync(
            """
            SELECT 
            	PQ.id, 
            	P.display_name, 
            	COALESCE(PQT.completed_at, NOW()) - PQ.created_at delivery_time,
            	PQ.status,
            	PQ.created_at,
            	PQT.completed_at
            FROM public.mod_player_quest PQ
            INNER JOIN public.mod_player_quest_task PQT ON PQT.quest_id = PQ.id
            INNER JOIN public.player P ON P.id = PQ.player_id
            WHERE PQT.type = 'deliver'
            ORDER BY completed_at DESC, delivery_time ASC
            """
        );
    }

    private QuestTaskItem MapToTaskItem(DbRow row)
    {
        return new QuestTaskItem(
            new QuestTaskId(
                row.id,
                row.task_id
            ),
            row.task_text,
            row.task_type,
            row.status,
            new Vec3
            {
                x = row.task_position_x,
                y = row.task_position_y,
                z = row.task_position_z
            },
            row.task_completed_at,
            JsonConvert.DeserializeObject<ScriptActionItem>(row.task_on_check_completed_script),
            Create(row.task_type, row.task_json_properties)
        );
    }

    private IQuestTaskItemDefinition Create(string taskType, string properties)
    {
        switch (taskType)
        {
            case QuestTaskItemType.Deliver:
                return JsonConvert.DeserializeObject<DeliverItemTaskDefinition>(properties);
            case QuestTaskItemType.Pickup:
                return JsonConvert.DeserializeObject<PickupItemTaskItemDefinition>(properties);
            case QuestTaskItemType.DeliverUnrestricted:
                return JsonConvert.DeserializeObject<DeliverItemsUnrestrictedTaskDefinition>(properties);
            default:
                throw new NotImplementedException();
        }
    }

    private PlayerQuestItem MapToModel(DbRow row)
    {
        return new PlayerQuestItem(
            row.id,
            row.original_quest_id,
            row.faction_id,
            (ulong)row.player_id,
            row.type,
            row.status,
            row.seed,
            JsonConvert.DeserializeObject<PlayerQuestItem.QuestProperties>(row.json_properties),
            row.created_at,
            row.expires_at,
            [],
            JsonConvert.DeserializeObject<ScriptActionItem>(row.on_success_script),
            JsonConvert.DeserializeObject<ScriptActionItem>(row.on_failure_script)
        );
    }

    private class DbRow
    {
        public Guid id { get; set; }
        public Guid original_quest_id { get; set; }
        public long player_id { get; set; }
        public long faction_id { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public int seed { get; set; }
        public string json_properties { get; set; }
        public DateTime created_at { get; set; }
        public DateTime expires_at { get; set; }
        public string on_success_script { get; set; }
        public string on_failure_script { get; set; }

        public Guid task_id { get; set; }
        public ulong task_base_construct_id { get; set; }
        public DateTime? task_completed_at { get; set; }
        public string task_json_properties { get; set; }
        public string task_on_check_completed_script { get; set; }
        public double task_position_x { get; set; }
        public double task_position_y { get; set; }
        public double task_position_z { get; set; }
        public string task_status { get; set; }
        public string task_text { get; set; }
        public string task_type { get; set; }
    }

    [GeneratedRegex("[^a0-z9\\-]")]
    private static partial Regex SanitizeNonAlphaNumerics();
}