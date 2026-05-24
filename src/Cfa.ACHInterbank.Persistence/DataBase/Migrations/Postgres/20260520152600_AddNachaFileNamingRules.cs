using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddNachaFileNamingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NachaFileNamingRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    SourceFinancialInstitutionId = table.Column<int>(type: "integer", nullable: true),
                    FileDirection = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NamePattern = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Extension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DailySequenceMin = table.Column<int>(type: "integer", nullable: false),
                    DailySequenceMax = table.Column<int>(type: "integer", nullable: false),
                    InternalFileIdMappingMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequiresNameHeaderEntityMatch = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NormativeSource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormativeReference = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaFileNamingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NachaFileNamingRules_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NachaFileNamingRules_FinancialInstitutions_SourceFinancialI~",
                        column: x => x.SourceFinancialInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NachaFileNamingRules_SourceFinancialInstitutionId",
                table: "NachaFileNamingRules",
                column: "SourceFinancialInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_NFNR_RuleLookup",
                table: "NachaFileNamingRules",
                columns: new[] { "ClearingHouseId", "FileDirection", "IsActive", "EffectiveFrom" });

            migrationBuilder.Sql("""
                INSERT INTO "NachaFileNamingRules"
                    ("ClearingHouseId","SourceFinancialInstitutionId","FileDirection","NamePattern","Extension",
                     "DailySequenceMin","DailySequenceMax","InternalFileIdMappingMode","RequiresNameHeaderEntityMatch",
                     "IsActive","EffectiveFrom","EffectiveTo","NormativeSource","NormativeReference","Notes","CreatedAt","UpdatedAt")
                SELECT ch."Id", fi."Id", 'Outbound', 'RRRRTTT.ZZZ.1', '',
                       1, 36, 'Alphanumeric36', true,
                       true, '2026-01-01T00:00:00Z'::timestamptz, null,
                       'MAN-004 ACH Colombia V32',
                       'Seccion 6.1.10.1; tabla identificador archivo; maximo 36 archivos diarios.',
                       'Regla parametrizada UAT/preproductiva: RRRRTTT.ZZZ.1; ZZZ 001-026=A-Z, 027-036=0-9.',
                       timezone('utc', now()), timezone('utc', now())
                FROM "ClearingHouses" ch
                CROSS JOIN LATERAL (
                    SELECT "Id" FROM "FinancialInstitutions" WHERE "IsDefaultSource" = true ORDER BY "Id" LIMIT 1
                ) fi
                WHERE ch."Code" IN ('ACH', 'ACHCOL')
                  AND NOT EXISTS (
                      SELECT 1 FROM "NachaFileNamingRules" r
                      WHERE r."ClearingHouseId" = ch."Id"
                        AND r."FileDirection" = 'Outbound'
                        AND r."NamePattern" = 'RRRRTTT.ZZZ.1'
                  );

                INSERT INTO "NachaFileNamingRules"
                    ("ClearingHouseId","SourceFinancialInstitutionId","FileDirection","NamePattern","Extension",
                     "DailySequenceMin","DailySequenceMax","InternalFileIdMappingMode","RequiresNameHeaderEntityMatch",
                     "IsActive","EffectiveFrom","EffectiveTo","NormativeSource","NormativeReference","Notes","CreatedAt","UpdatedAt")
                SELECT ch."Id", fi."Id", 'Outbound', 'RRRRTTT.ZZZ.1', '',
                       1, 36, 'Alphanumeric36', true,
                       true, '2026-01-01T00:00:00Z'::timestamptz, null,
                       'Ejemplos CENIT disponibles en el proyecto / pendiente homologacion normativa formal',
                       'Evidencia de ejemplos CENIT del proyecto; homologacion formal pendiente.',
                       'Regla parametrizada para no hard-codear CENIT. Usa el mismo patron observado RRRRTTT.ZZZ.1 mientras se homologa formalmente.',
                       timezone('utc', now()), timezone('utc', now())
                FROM "ClearingHouses" ch
                CROSS JOIN LATERAL (
                    SELECT "Id" FROM "FinancialInstitutions" WHERE "IsDefaultSource" = true ORDER BY "Id" LIMIT 1
                ) fi
                WHERE ch."Code" = 'CENIT'
                  AND NOT EXISTS (
                      SELECT 1 FROM "NachaFileNamingRules" r
                      WHERE r."ClearingHouseId" = ch."Id"
                        AND r."FileDirection" = 'Outbound'
                        AND r."NamePattern" = 'RRRRTTT.ZZZ.1'
                  );
                """);

            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    source_fi_id integer;
                    origin_dfi text;
                    company_entry_id integer;
                    ach_cycle_id text;
                    cenit_cycle_id text;
                    ach_batch_id integer;
                    cenit_batch_id integer;
                    ach_dest_id integer;
                    cenit_dest_id integer;
                    tx_id integer;
                    matured_date timestamptz := (current_date - interval '7 day');
                BEGIN
                    SELECT "Id", ("RoutingNumber" || "TransitCode")
                    INTO source_fi_id, origin_dfi
                    FROM "FinancialInstitutions"
                    WHERE "IsDefaultSource" = true
                    ORDER BY "Id"
                    LIMIT 1;

                    SELECT "Id" INTO company_entry_id
                    FROM "CompanyEntryDescription"
                    WHERE "Term" = 'PAGOS PSE'
                    ORDER BY "Id"
                    LIMIT 1;

                    SELECT "Id" INTO ach_dest_id
                    FROM "FinancialInstitutions"
                    WHERE "Name" = 'Banco UAT Destino'
                    ORDER BY "Id"
                    LIMIT 1;

                    SELECT "Id" INTO cenit_dest_id
                    FROM "FinancialInstitutions"
                    WHERE "Name" = 'Banco UAT Destino CENIT'
                    ORDER BY "Id"
                    LIMIT 1;

                    SELECT c."Id" INTO ach_cycle_id
                    FROM "AchCycles" c
                    JOIN "ClearingHouses" ch ON ch."Id" = c."ClearingHouseId"
                    WHERE ch."Code" IN ('ACH', 'ACHCOL')
                      AND c."ProcessingDate" = current_date
                    ORDER BY c."CycleName"
                    LIMIT 1;

                    SELECT c."Id" INTO cenit_cycle_id
                    FROM "AchCycles" c
                    JOIN "ClearingHouses" ch ON ch."Id" = c."ClearingHouseId"
                    WHERE ch."Code" = 'CENIT'
                      AND c."ProcessingDate" = current_date
                    ORDER BY c."CycleName"
                    LIMIT 1;

                    IF source_fi_id IS NOT NULL AND company_entry_id IS NOT NULL AND ach_cycle_id IS NOT NULL AND ach_dest_id IS NOT NULL THEN
                        SELECT "Id" INTO ach_batch_id FROM "AchBatches" WHERE "AchCycleId" = ach_cycle_id ORDER BY "Id" LIMIT 1;
                        IF ach_batch_id IS NULL THEN
                            INSERT INTO "AchBatches"
                                ("AchCycleId","ServiceClassCode","CompanyName","CompanyIdentification","CompanyEntryDescription",
                                 "CompanyEntryDescriptionId","OriginOrOdfi","EffectiveEntryDate","BatchSequenceNumber",
                                 "TotalDebitAmount","TotalCreditAmount","CreatedAt","UpdatedAt")
                            VALUES
                                (ach_cycle_id,'200','UAT SINT','900000001','PAGOS PSE',
                                 company_entry_id,origin_dfi,current_date,1,0,0,timezone('utc', now()),timezone('utc', now()))
                            RETURNING "Id" INTO ach_batch_id;
                        END IF;

                        IF NOT EXISTS (SELECT 1 FROM "AchTransactions" WHERE "Reference" = 'UAT-ACH-PRE-MATURED-001') THEN
                            INSERT INTO "AchTransactions"
                                ("Amount","TransactionExternalId","Reference","Type","TransactionCode","ServiceClassCode",
                                 "CompanyEntryDescriptionId","CompanyName","CompanyIdentification","OriginatingDFI","ReceivingDFI",
                                 "TraceNumber","TraceSequenceNumber","EffectiveEntryDate","AddendaRecordIndicator","IsPrenotification",
                                 "State","StateChangedAtUtc","ContrapartidasResponseCode","ReturnReasonCode","OriginalTraceRef",
                                 "RecipientIdNumber","DiscretionaryData","SourceAccountNumber","DestinationAccountNumber",
                                 "SourceInstitutionId","DestinationInstitutionId","AchCycleId","AchBatchId","CustomerId","CreatedAt","UpdatedAt")
                            VALUES
                                (0,'UAT-ACH-PRE-MATURED-001','UAT-ACH-PRE-MATURED-001','Prenotification','28','200',
                                 company_entry_id,'UAT SINT','900000001',origin_dfi,'99999002',
                                 origin_dfi || '090001',90001,matured_date,false,true,
                                 'Pending',timezone('utc', now()),'','','','900000101','UT','0000001101','0000001102',
                                 source_fi_id,ach_dest_id,ach_cycle_id,ach_batch_id,null,timezone('utc', now()),timezone('utc', now()))
                            RETURNING "Id" INTO tx_id;

                            INSERT INTO "AchTransactionStateEvents"
                                ("AchTransactionId","FromState","ToState","Source","ReasonCode","PayloadJson","CreatedAt","UpdatedAt")
                            VALUES
                                (tx_id,'Pending','Pending','System','UAT_PRENOTE_CREATED',
                                 '{"scope":"UAT","control":"PRENOTE_UAT_PRECONDITION_CREATED","externalTransmission":false}',
                                 timezone('utc', now()),timezone('utc', now()));
                        END IF;
                    END IF;

                    IF source_fi_id IS NOT NULL AND company_entry_id IS NOT NULL AND cenit_cycle_id IS NOT NULL AND cenit_dest_id IS NOT NULL THEN
                        SELECT "Id" INTO cenit_batch_id FROM "AchBatches" WHERE "AchCycleId" = cenit_cycle_id ORDER BY "Id" LIMIT 1;
                        IF cenit_batch_id IS NULL THEN
                            INSERT INTO "AchBatches"
                                ("AchCycleId","ServiceClassCode","CompanyName","CompanyIdentification","CompanyEntryDescription",
                                 "CompanyEntryDescriptionId","OriginOrOdfi","EffectiveEntryDate","BatchSequenceNumber",
                                 "TotalDebitAmount","TotalCreditAmount","CreatedAt","UpdatedAt")
                            VALUES
                                (cenit_cycle_id,'200','UAT SINT','900000001','PAGOS PSE',
                                 company_entry_id,origin_dfi,current_date,1,0,0,timezone('utc', now()),timezone('utc', now()))
                            RETURNING "Id" INTO cenit_batch_id;
                        END IF;

                        IF NOT EXISTS (SELECT 1 FROM "AchTransactions" WHERE "Reference" = 'UAT-CEN-PRE-MATURED-001') THEN
                            INSERT INTO "AchTransactions"
                                ("Amount","TransactionExternalId","Reference","Type","TransactionCode","ServiceClassCode",
                                 "CompanyEntryDescriptionId","CompanyName","CompanyIdentification","OriginatingDFI","ReceivingDFI",
                                 "TraceNumber","TraceSequenceNumber","EffectiveEntryDate","AddendaRecordIndicator","IsPrenotification",
                                 "State","StateChangedAtUtc","ContrapartidasResponseCode","ReturnReasonCode","OriginalTraceRef",
                                 "RecipientIdNumber","DiscretionaryData","SourceAccountNumber","DestinationAccountNumber",
                                 "SourceInstitutionId","DestinationInstitutionId","AchCycleId","AchBatchId","CustomerId","CreatedAt","UpdatedAt")
                            VALUES
                                (0,'UAT-CEN-PRE-MATURED-001','UAT-CEN-PRE-MATURED-001','Prenotification','28','200',
                                 company_entry_id,'UAT SINT','900000001',origin_dfi,'99998002',
                                 origin_dfi || '090002',90002,matured_date,false,true,
                                 'Pending',timezone('utc', now()),'','','','900000102','UT','0000001201','0000001202',
                                 source_fi_id,cenit_dest_id,cenit_cycle_id,cenit_batch_id,null,timezone('utc', now()),timezone('utc', now()))
                            RETURNING "Id" INTO tx_id;

                            INSERT INTO "AchTransactionStateEvents"
                                ("AchTransactionId","FromState","ToState","Source","ReasonCode","PayloadJson","CreatedAt","UpdatedAt")
                            VALUES
                                (tx_id,'Pending','Pending','System','UAT_PRENOTE_CREATED',
                                 '{"scope":"UAT","control":"PRENOTE_UAT_PRECONDITION_CREATED","externalTransmission":false}',
                                 timezone('utc', now()),timezone('utc', now()));
                        END IF;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "AchTransactionStateEvents"
                WHERE "AchTransactionId" IN (
                    SELECT "Id" FROM "AchTransactions"
                    WHERE "Reference" IN ('UAT-ACH-PRE-MATURED-001','UAT-CEN-PRE-MATURED-001')
                );

                DELETE FROM ONLY "AchTransactions"
                WHERE "Reference" IN ('UAT-ACH-PRE-MATURED-001','UAT-CEN-PRE-MATURED-001');
                """);

            migrationBuilder.DropTable(
                name: "NachaFileNamingRules");
        }
    }
}
