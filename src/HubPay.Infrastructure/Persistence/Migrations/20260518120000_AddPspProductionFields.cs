using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubPay.Infrastructure.Persistence.Migrations;

public partial class AddPspProductionFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CustomerPhone",
            table: "transactions",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PspMetadataJson",
            table: "transactions",
            type: "jsonb",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CustomerPhone", table: "transactions");
        migrationBuilder.DropColumn(name: "PspMetadataJson", table: "transactions");
    }
}
