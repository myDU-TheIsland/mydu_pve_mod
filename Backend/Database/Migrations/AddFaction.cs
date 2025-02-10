using FluentMigrator;

namespace Mod.DynamicEncounters.Database.Migrations;

[Migration(65)]
public class AddFaction : Migration
{
    private const string ConstructTable = "construct";
    private const string OwnershipTable = "ownership";
    
    public override void Up()
    {
        Alter.Table(ConstructTable)
            .AddColumn("faction_id").AsInt64().Nullable();
        Alter.Table(OwnershipTable)
            .AddColumn("faction_id").AsInt64().Nullable();
    }

    public override void Down()
    {
        Delete.Column("faction_id")
            .FromTable(ConstructTable);
        Delete.Column("faction_id")
            .FromTable(OwnershipTable);
    }
}