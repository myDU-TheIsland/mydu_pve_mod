using FluentMigrator;

namespace Mod.DynamicEncounters.Database.Migrations;

[Migration(52)]
public class AddNpcTableFields : Migration
{
    public const string NpcDefTable = "mod_npc_def";
    
    public override void Up()
    {
        Alter.Table(NpcDefTable)
            .AddColumn("active").AsBoolean().NotNullable().WithDefaultValue(true);
    }

    public override void Down()
    {
        Delete.Column("active").FromTable(NpcDefTable);
    }
}