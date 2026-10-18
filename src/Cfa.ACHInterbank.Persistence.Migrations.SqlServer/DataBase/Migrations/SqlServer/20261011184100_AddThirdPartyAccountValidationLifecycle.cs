using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    public partial class AddThirdPartyAccountValidationLifecycle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "PrenotificationResponseDeadlineMode", table: "ClearingHouseTransactionRules", type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "NotApplicable");
            migrationBuilder.AddColumn<int>(name: "PrenotificationResponseWindowCycles", table: "ClearingHouseTransactionRules", type: "int", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "TacitAcceptanceEnabled", table: "ClearingHouseTransactionRules", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<int>(name: "ValidationRetentionMonthsAfterLastMonetaryUse", table: "ClearingHouseTransactionRules", type: "int", nullable: true);

            migrationBuilder.CreateTable(
                name: "ThirdPartyAccountValidations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ThirdPartyAccountId = table.Column<long>(type: "bigint", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    TransactionNature = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: ""),
                    ValidationType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentAttemptId = table.Column<long>(type: "bigint", nullable: true),
                    PolicyRuleId = table.Column<int>(type: "int", nullable: true),
                    PolicyEffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastMonetaryUseAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThirdPartyAccountValidations", x => x.Id);
                    table.ForeignKey(name: "FK_ThirdPartyAccountValidations_ClearingHouses_ClearingHouseId", column: x => x.ClearingHouseId, principalTable: "ClearingHouses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_ThirdPartyAccountValidations_ClearingHouseTransactionRules_PolicyRuleId", column: x => x.PolicyRuleId, principalTable: "ClearingHouseTransactionRules", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_ThirdPartyAccountValidations_ThirdPartyAccounts_ThirdPartyAccountId", column: x => x.ThirdPartyAccountId, principalTable: "ThirdPartyAccounts", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrenotificationValidationAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ThirdPartyAccountValidationId = table.Column<long>(type: "bigint", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    AchTransactionId = table.Column<int>(type: "int", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResponseDeadlineAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReturnCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReturnDetail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EvidenceReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CorrelationReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceCycleId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceFileReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrenotificationValidationAttempts", x => x.Id);
                    table.ForeignKey(name: "FK_PrenotificationValidationAttempts_AchTransactions_AchTransactionId", column: x => x.AchTransactionId, principalTable: "AchTransactions", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_PrenotificationValidationAttempts_ThirdPartyAccountValidations_ThirdPartyAccountValidationId", column: x => x.ThirdPartyAccountValidationId, principalTable: "ThirdPartyAccountValidations", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_TPAV_PolicyRuleId", table: "ThirdPartyAccountValidations", column: "PolicyRuleId");
            migrationBuilder.CreateIndex(name: "IX_ThirdPartyAccountValidations_ClearingHouseId", table: "ThirdPartyAccountValidations", column: "ClearingHouseId");
            migrationBuilder.CreateIndex(name: "UX_TPAV_Account_Chamber_Nature_Service_Type", table: "ThirdPartyAccountValidations", columns: new[] { "ThirdPartyAccountId", "ClearingHouseId", "TransactionNature", "ServiceType", "ValidationType" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_PVA_AchTransactionId", table: "PrenotificationValidationAttempts", column: "AchTransactionId");
            migrationBuilder.CreateIndex(name: "UX_PVA_IdempotencyKey", table: "PrenotificationValidationAttempts", column: "IdempotencyKey", unique: true);
            migrationBuilder.CreateIndex(name: "UX_PVA_Validation_AttemptNumber", table: "PrenotificationValidationAttempts", columns: new[] { "ThirdPartyAccountValidationId", "AttemptNumber" }, unique: true);

            migrationBuilder.Sql("""
                UPDATE r
                SET r.NormativeSource = 'DDS-DIS-MAN-004 ACH Colombia V35',
                    r.PrenotificationResponseDeadlineMode = 'CycleCount',
                    r.PrenotificationResponseWindowCycles = 4,
                    r.TacitAcceptanceEnabled = 1,
                    r.PrenotificationLeadBusinessDays = CASE WHEN r.TransactionNature = 'Credit' THEN 3 ELSE r.PrenotificationLeadBusinessDays END
                FROM ClearingHouseTransactionRules r
                INNER JOIN ClearingHouses ch ON ch.Id = r.ClearingHouseId
                WHERE ch.Code IN ('ACHCOL', 'ACH')
                  AND r.NormativeSource = 'MAN-004 ACH Colombia V32';

                UPDATE r
                SET r.PrenotificationResponseDeadlineMode = 'OperationalDayEnd',
                    r.PrenotificationResponseWindowCycles = NULL,
                    r.TacitAcceptanceEnabled = 1,
                    r.ValidationRetentionMonthsAfterLastMonetaryUse = CASE WHEN r.TransactionNature = 'Debit' THEN 13 ELSE NULL END,
                    r.NormativeReference = '4.7 Prenotificaciones; 4.8 Devolución de una prenotificación'
                FROM ClearingHouseTransactionRules r
                INNER JOIN ClearingHouses ch ON ch.Id = r.ClearingHouseId
                WHERE ch.Code = 'CENIT'
                  AND r.NormativeSource = 'CENIT DSP-152 Anexo 2';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PrenotificationValidationAttempts");
            migrationBuilder.DropTable(name: "ThirdPartyAccountValidations");
            migrationBuilder.DropColumn(name: "PrenotificationResponseDeadlineMode", table: "ClearingHouseTransactionRules");
            migrationBuilder.DropColumn(name: "PrenotificationResponseWindowCycles", table: "ClearingHouseTransactionRules");
            migrationBuilder.DropColumn(name: "TacitAcceptanceEnabled", table: "ClearingHouseTransactionRules");
            migrationBuilder.DropColumn(name: "ValidationRetentionMonthsAfterLastMonetaryUse", table: "ClearingHouseTransactionRules");
        }
    }
}
