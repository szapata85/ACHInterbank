using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddCfaOperationalTransactionRestrictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CfaOperationalTransactionRestrictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: true),
                    ClearingHouseCycleConfigId = table.Column<int>(type: "int", nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: true),
                    IsPrenotification = table.Column<bool>(type: "bit", nullable: true),
                    MaxAmountPerTransaction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxAmountPerCycle = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxTransactionsPerCycle = table.Column<int>(type: "int", nullable: true),
                    AllowedAccountTypes = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfaOperationalTransactionRestrictions", x => x.Id);
                    table.CheckConstraint("CK_CfaOperationalTransactionRestrictions_EffectiveRange", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.CheckConstraint("CK_CfaOperationalTransactionRestrictions_MaxAmountPerCycle", "[MaxAmountPerCycle] IS NULL OR [MaxAmountPerCycle] >= 0");
                    table.CheckConstraint("CK_CfaOperationalTransactionRestrictions_MaxAmountPerTransaction", "[MaxAmountPerTransaction] IS NULL OR [MaxAmountPerTransaction] >= 0");
                    table.CheckConstraint("CK_CfaOperationalTransactionRestrictions_MaxTransactionsPerCycle", "[MaxTransactionsPerCycle] IS NULL OR [MaxTransactionsPerCycle] >= 0");
                    table.CheckConstraint("CK_CfaOperationalTransactionRestrictions_Scope", "NOT ([ClearingHouseId] IS NOT NULL AND [ClearingHouseCycleConfigId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CfaOperationalTransactionRestrictions_ClearingHouseCycleConfigs_ClearingHouseCycleConfigId",
                        column: x => x.ClearingHouseCycleConfigId,
                        principalTable: "ClearingHouseCycleConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfaOperationalTransactionRestrictions_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CfaOperationalRestrictions_Resolve",
                table: "CfaOperationalTransactionRestrictions",
                columns: new[] { "ClearingHouseId", "ClearingHouseCycleConfigId", "TransactionType", "IsPrenotification", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_CfaOperationalTransactionRestrictions_ClearingHouseCycleConfigId",
                table: "CfaOperationalTransactionRestrictions",
                column: "ClearingHouseCycleConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CfaOperationalTransactionRestrictions");
        }
    }
}
