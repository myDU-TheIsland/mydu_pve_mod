using FluentMigrator;

namespace Mod.DynamicEncounters.Database.Migrations;

[Migration(54)]
public class AddLootProperties : Migration
{
    private const string LootTable = "mod_loot_def";
    
    public override void Up()
    {
        Alter.Table(LootTable)
            .AddColumn("json_properties").AsCustom("jsonb").NotNullable().WithDefaultValue("{}");
    }

    public override void Down()
    {
        Delete.Column("json_properties")
            .FromTable(LootTable);
    }
}