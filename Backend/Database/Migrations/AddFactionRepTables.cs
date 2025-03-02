using FluentMigrator;
using Mod.DynamicEncounters.Database.Helpers;

namespace Mod.DynamicEncounters.Database.Migrations;

[Migration(66)]
public class AddFactionRepTables : Migration
{
    private const string Faction = "mod_faction";
    private const string FactionRep = "mod_faction_rep";
    
    public override void Up()
    {
        Create.Table(FactionRep)
            .WithColumn("player_id").AsInt64().NotNullable()
            .WithColumn("faction_id").AsInt64().ForeignKey(Faction, "id")
            .WithColumn("reputation").AsInt64().Nullable().WithDefaultValue(0)
            .WithColumn("updated_at").AsDateTimeUtc().WithDefault(SystemMethods.CurrentDateTime);

        Execute.Sql("""
                    CREATE UNIQUE INDEX idx_mod_faction_rep_player_faction
                    ON mod_faction_rep (player_id, faction_id);
                    """);
    }

    public override void Down()
    {
        Delete.Table(FactionRep);
    }
}