using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubPay.Infrastructure.Persistence.Migrations;

public partial class AddPspConfigurationTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "psp_provider_configurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Scheme = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                SettingsJson = table.Column<string>(type: "jsonb", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_psp_provider_configurations", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_psp_provider_configurations_Scheme",
            table: "psp_provider_configurations",
            column: "Scheme",
            unique: true);

        migrationBuilder.CreateTable(
            name: "psp_merchant_configurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Scheme = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                MerchantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SettingsJson = table.Column<string>(type: "jsonb", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_psp_merchant_configurations", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_psp_merchant_configurations_Scheme_MerchantId",
            table: "psp_merchant_configurations",
            columns: new[] { "Scheme", "MerchantId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "psp_merchant_configurations");
        migrationBuilder.DropTable(name: "psp_provider_configurations");
    }
}
