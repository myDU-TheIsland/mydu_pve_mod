using FluentMigrator;

namespace Mod.DynamicEncounters.Database.Migrations;

[Migration(51)]
public class AddNpcTable : Migration
{
    public const string NpcDefTable = "mod_npc_def";
    private const string FactionTable = "mod_faction";
    
    public override void Up()
    {
        Create.Table(NpcDefTable)
            .WithColumn("id").AsGuid().WithDefault(SystemMethods.NewGuid).PrimaryKey()
            .WithColumn("name").AsString()
            .WithColumn("faction_id").AsInt64().ForeignKey(FactionTable, "id");
    }

    public override void Down()
    {
        Delete.Table(NpcDefTable);
    }
}