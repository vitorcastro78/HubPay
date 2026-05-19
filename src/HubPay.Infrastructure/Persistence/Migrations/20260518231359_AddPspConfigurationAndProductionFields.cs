using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubPay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPspConfigurationAndProductionFields : Migration
    {
        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "psp_merchant_configurations");
            migrationBuilder.DropTable(name: "psp_provider_configurations");
            migrationBuilder.DropColumn(name: "PspMetadataJson", table: "transactions");
            migrationBuilder.DropColumn(name: "CustomerPhone", table: "transactions");
        }
    }
}
