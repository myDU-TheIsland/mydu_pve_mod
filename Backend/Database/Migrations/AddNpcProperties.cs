using FluentMigrator;

namespace Mod.DynamicEncounters.Database.Migrations;

[Migration(53)]
public class AddNpcProperties : Migration
{
    public const string NpcDefTable = "mod_npc_def";
    
    public override void Up()
    {
        Alter.Table(NpcDefTable)
            .AddColumn("json_properties").AsCustom("jsonb").NotNullable().WithDefaultValue("{}");
    }

    public override void Down()
    {
        Delete.Column("json_properties").FromTable(NpcDefTable);
    }
}