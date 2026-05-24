using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddClearingHouseTransactionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClearingHouseTransactionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    TransactionNature = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequiresPrenotification = table.Column<bool>(type: "boolean", nullable: false),
                    PrenotificationMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequiresReceiverIdentificationValidation = table.Column<bool>(type: "boolean", nullable: false),
                    ReceiverIdentificationValidationMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AppliesToNachaExport = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AppliesToMonetaryTransactions = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NormativeSource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormativeReference = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClearingHouseTransactionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClearingHouseTransactionRules_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CHTR_RuleLookup",
                table: "ClearingHouseTransactionRules",
                columns: new[] { "ClearingHouseId", "TransactionNature", "TransactionType", "AppliesToNachaExport", "AppliesToMonetaryTransactions", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClearingHouseTransactionRules");
        }
    }
}
