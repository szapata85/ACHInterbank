using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class IntegrationsAddContractVersioningBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FunctionalCategory",
                table: "IntegrationMethods",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FunctionalDescription",
                table: "IntegrationMethods",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RiskLevel",
                table: "IntegrationMethods",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ContractVersionId",
                table: "IntegrationMethodParameters",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxOccurs",
                table: "IntegrationMethodParameters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinOccurs",
                table: "IntegrationMethodParameters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Nillable",
                table: "IntegrationMethodParameters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SupportStatus",
                table: "IntegrationMethodParameters",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Supported");

            migrationBuilder.AddColumn<string>(
                name: "XmlElementName",
                table: "IntegrationMethodParameters",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "XsdType",
                table: "IntegrationMethodParameters",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "string");

            migrationBuilder.AddColumn<Guid>(
                name: "ContractVersionId",
                table: "IntegrationMappingSets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                table: "IntegrationMappingSets",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "IntegrationContractVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MethodId = table.Column<int>(type: "integer", nullable: false),
                    VersionLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OperationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetNamespace = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SoapAction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceDocumentName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    SourceDocumentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SourceReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    ImportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImportedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActivatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationContractVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationContractVersions_IntegrationMethods_MethodId",
                        column: x => x.MethodId,
                        principalTable: "IntegrationMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationContractVersions_SourceDocumentHash",
                table: "IntegrationContractVersions",
                column: "SourceDocumentHash");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationContractVersions_Method_Status",
                table: "IntegrationContractVersions",
                columns: new[] { "MethodId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_IntegrationContractVersions_Method_VersionLabel",
                table: "IntegrationContractVersions",
                columns: new[] { "MethodId", "VersionLabel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMethodParameters_ContractVersionId_ParameterPath",
                table: "IntegrationMethodParameters",
                columns: new[] { "ContractVersionId", "ParameterPath" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingSets_ContractVersionId",
                table: "IntegrationMappingSets",
                column: "ContractVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingSets_MethodId_ContractVersionId_Version",
                table: "IntegrationMappingSets",
                columns: new[] { "MethodId", "ContractVersionId", "Version" });

            migrationBuilder.Sql("""
                INSERT INTO "IntegrationMethods"
                    ("Code", "DisplayName", "FunctionalDescription", "FunctionalCategory", "RiskLevel", "SoapClientCode", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT 'WSCFAACH.Proc_Contrapartidas', 'Proc_Contrapartidas', '', '', '', 'WscfaachSoapClient', TRUE,
                       TIMESTAMPTZ '2026-10-12 23:10:00+00', TIMESTAMPTZ '2026-10-12 23:10:00+00'
                WHERE NOT EXISTS (SELECT 1 FROM "IntegrationMethods" WHERE "Code" = 'WSCFAACH.Proc_Contrapartidas');

                INSERT INTO "IntegrationMethods"
                    ("Code", "DisplayName", "FunctionalDescription", "FunctionalCategory", "RiskLevel", "SoapClientCode", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT 'WSCFAACH.Proc_Transacciones', 'Proc_Transacciones', '', '', '', 'WscfaachSoapClient', TRUE,
                       TIMESTAMPTZ '2026-10-12 23:10:00+00', TIMESTAMPTZ '2026-10-12 23:10:00+00'
                WHERE NOT EXISTS (SELECT 1 FROM "IntegrationMethods" WHERE "Code" = 'WSCFAACH.Proc_Transacciones');

                INSERT INTO "IntegrationMethods"
                    ("Code", "DisplayName", "FunctionalDescription", "FunctionalCategory", "RiskLevel", "SoapClientCode", "IsActive", "CreatedAt", "UpdatedAt")
                SELECT 'WSAXON.RegistrarRespuestaTransaccion', 'RegistrarRespuestaTransaccion', '', '', '', 'WsAxonRespuestaTransaccionesSoapClient', TRUE,
                       TIMESTAMPTZ '2026-10-12 23:10:00+00', TIMESTAMPTZ '2026-10-12 23:10:00+00'
                WHERE NOT EXISTS (SELECT 1 FROM "IntegrationMethods" WHERE "Code" = 'WSAXON.RegistrarRespuestaTransaccion');

                UPDATE "IntegrationMethods"
                SET "FunctionalDescription" = CASE "Code"
                        WHEN 'WSCFAACH.Proc_Contrapartidas' THEN 'Débitos originados en CFA enviados mediante Proc_Contrapartidas.'
                        WHEN 'WSCFAACH.Proc_Transacciones' THEN 'Créditos recibidos en CFA procesados mediante Proc_Transacciones.'
                        WHEN 'WSAXON.RegistrarRespuestaTransaccion' THEN 'Respuestas funcionales registradas para transacciones mediante WSAXON.'
                        ELSE "FunctionalDescription"
                    END,
                    "FunctionalCategory" = CASE "Code"
                        WHEN 'WSCFAACH.Proc_Contrapartidas' THEN 'Débitos'
                        WHEN 'WSCFAACH.Proc_Transacciones' THEN 'Créditos'
                        WHEN 'WSAXON.RegistrarRespuestaTransaccion' THEN 'Respuestas'
                        ELSE "FunctionalCategory"
                    END,
                    "RiskLevel" = 'No clasificado',
                    "UpdatedAt" = GREATEST("UpdatedAt", TIMESTAMPTZ '2026-10-12 23:10:00+00')
                WHERE "Code" IN ('WSCFAACH.Proc_Contrapartidas', 'WSCFAACH.Proc_Transacciones', 'WSAXON.RegistrarRespuestaTransaccion');

                INSERT INTO "IntegrationContractVersions"
                    ("Id", "MethodId", "VersionLabel", "Status", "OperationName", "TargetNamespace", "SoapAction", "SourceType",
                     "SourceDocumentName", "SourceDocumentHash", "SourceReference", "Notes", "Revision", "ImportedAtUtc", "ImportedBy",
                     "ActivatedAtUtc", "ActivatedBy", "CreatedAt", "UpdatedAt")
                SELECT '39b5693c-407b-505c-8e8a-e49e8558a82e'::uuid, m."Id", 'baseline-v1', 3,
                       'Proc_Contrapartidas', 'http://tempuri.org/', 'http://tempuri.org/IWSCFAACH/Proc_Contrapartidas', 'BaselineMigration',
                       NULL, NULL, 'CFA-SOAP-UX-MAP-001/P0',
                       'Baseline derivado del comportamiento SOAP caracterizado en P0; no implica importación WSDL.', 1, NULL, NULL,
                       TIMESTAMPTZ '2026-10-12 23:10:00+00', 'CFA-SOAP-UX-MAP-001/P1.2',
                       TIMESTAMPTZ '2026-10-12 23:10:00+00', TIMESTAMPTZ '2026-10-12 23:10:00+00'
                FROM "IntegrationMethods" m
                WHERE m."Code" = 'WSCFAACH.Proc_Contrapartidas'
                  AND NOT EXISTS (
                      SELECT 1 FROM "IntegrationContractVersions" cv
                      WHERE cv."MethodId" = m."Id" AND cv."VersionLabel" = 'baseline-v1');

                INSERT INTO "IntegrationContractVersions"
                    ("Id", "MethodId", "VersionLabel", "Status", "OperationName", "TargetNamespace", "SoapAction", "SourceType",
                     "SourceDocumentName", "SourceDocumentHash", "SourceReference", "Notes", "Revision", "ImportedAtUtc", "ImportedBy",
                     "ActivatedAtUtc", "ActivatedBy", "CreatedAt", "UpdatedAt")
                SELECT '9f3a13b6-eb79-5e43-803b-4c97b71a0f5d'::uuid, m."Id", 'baseline-v1', 3,
                       'Proc_Transacciones', 'http://tempuri.org/', 'http://tempuri.org/IWSCFAACH/Proc_Transacciones', 'BaselineMigration',
                       NULL, NULL, 'CFA-SOAP-UX-MAP-001/P0',
                       'Baseline derivado del comportamiento SOAP caracterizado en P0; no implica importación WSDL.', 1, NULL, NULL,
                       TIMESTAMPTZ '2026-10-12 23:10:00+00', 'CFA-SOAP-UX-MAP-001/P1.2',
                       TIMESTAMPTZ '2026-10-12 23:10:00+00', TIMESTAMPTZ '2026-10-12 23:10:00+00'
                FROM "IntegrationMethods" m
                WHERE m."Code" = 'WSCFAACH.Proc_Transacciones'
                  AND NOT EXISTS (
                      SELECT 1 FROM "IntegrationContractVersions" cv
                      WHERE cv."MethodId" = m."Id" AND cv."VersionLabel" = 'baseline-v1');

                INSERT INTO "IntegrationContractVersions"
                    ("Id", "MethodId", "VersionLabel", "Status", "OperationName", "TargetNamespace", "SoapAction", "SourceType",
                     "SourceDocumentName", "SourceDocumentHash", "SourceReference", "Notes", "Revision", "ImportedAtUtc", "ImportedBy",
                     "ActivatedAtUtc", "ActivatedBy", "CreatedAt", "UpdatedAt")
                SELECT '4a093eb5-beed-5b15-bc7c-5768c0e0547e'::uuid, m."Id", 'baseline-v1', 3,
                       'RegistrarRespuestaTransaccion', 'http://tempuri.org/', 'http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion', 'BaselineMigration',
                       NULL, NULL, 'CFA-SOAP-UX-MAP-001/P0',
                       'Baseline derivado del comportamiento SOAP caracterizado en P0; conserva los siete parámetros actuales.', 1, NULL, NULL,
                       TIMESTAMPTZ '2026-10-12 23:10:00+00', 'CFA-SOAP-UX-MAP-001/P1.2',
                       TIMESTAMPTZ '2026-10-12 23:10:00+00', TIMESTAMPTZ '2026-10-12 23:10:00+00'
                FROM "IntegrationMethods" m
                WHERE m."Code" = 'WSAXON.RegistrarRespuestaTransaccion'
                  AND NOT EXISTS (
                      SELECT 1 FROM "IntegrationContractVersions" cv
                      WHERE cv."MethodId" = m."Id" AND cv."VersionLabel" = 'baseline-v1');

                UPDATE "IntegrationMethodParameters" p
                SET "ContractVersionId" = cv."Id",
                    "XmlElementName" = p."ParameterPath",
                    "XsdType" = CASE WHEN NULLIF(BTRIM(p."DataType"), '') IS NULL THEN 'string' ELSE p."DataType" END,
                    "Nillable" = FALSE,
                    "MinOccurs" = CASE WHEN p."Required" THEN 1 ELSE 0 END,
                    "MaxOccurs" = 1,
                    "SupportStatus" = 'Supported'
                FROM "IntegrationMethods" m
                INNER JOIN "IntegrationContractVersions" cv
                    ON cv."MethodId" = m."Id" AND cv."VersionLabel" = 'baseline-v1'
                WHERE p."MethodId" = m."Id"
                  AND m."Code" IN ('WSCFAACH.Proc_Contrapartidas', 'WSCFAACH.Proc_Transacciones', 'WSAXON.RegistrarRespuestaTransaccion');

                UPDATE "IntegrationMappingSets" s
                SET "ContractVersionId" = cv."Id",
                    "Revision" = CASE WHEN s."Revision" < 1 THEN 1 ELSE s."Revision" END
                FROM "IntegrationMethods" m
                INNER JOIN "IntegrationContractVersions" cv
                    ON cv."MethodId" = m."Id" AND cv."VersionLabel" = 'baseline-v1'
                WHERE s."MethodId" = m."Id"
                  AND m."Code" IN ('WSCFAACH.Proc_Contrapartidas', 'WSCFAACH.Proc_Transacciones', 'WSAXON.RegistrarRespuestaTransaccion');
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationMethodParameters_IntegrationContractVersions_ContractVersionId",
                table: "IntegrationMethodParameters",
                column: "ContractVersionId",
                principalTable: "IntegrationContractVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationMappingSets_IntegrationContractVersions_ContractVersionId",
                table: "IntegrationMappingSets",
                column: "ContractVersionId",
                principalTable: "IntegrationContractVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "IntegrationContractVersions"
                        WHERE NOT ("SourceType" = 'BaselineMigration' AND "VersionLabel" = 'baseline-v1')
                    ) THEN
                        RAISE EXCEPTION 'P1.2 downgrade refused: non-baseline contract version data exists.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_IntegrationMethodParameters_IntegrationContractVersions_ContractVersionId",
                table: "IntegrationMethodParameters");

            migrationBuilder.DropForeignKey(
                name: "FK_IntegrationMappingSets_IntegrationContractVersions_ContractVersionId",
                table: "IntegrationMappingSets");

            migrationBuilder.DropTable(name: "IntegrationContractVersions");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationMethodParameters_ContractVersionId_ParameterPath",
                table: "IntegrationMethodParameters");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationMappingSets_ContractVersionId",
                table: "IntegrationMappingSets");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationMappingSets_MethodId_ContractVersionId_Version",
                table: "IntegrationMappingSets");

            migrationBuilder.DropColumn(name: "FunctionalCategory", table: "IntegrationMethods");
            migrationBuilder.DropColumn(name: "FunctionalDescription", table: "IntegrationMethods");
            migrationBuilder.DropColumn(name: "RiskLevel", table: "IntegrationMethods");

            migrationBuilder.DropColumn(name: "ContractVersionId", table: "IntegrationMethodParameters");
            migrationBuilder.DropColumn(name: "MaxOccurs", table: "IntegrationMethodParameters");
            migrationBuilder.DropColumn(name: "MinOccurs", table: "IntegrationMethodParameters");
            migrationBuilder.DropColumn(name: "Nillable", table: "IntegrationMethodParameters");
            migrationBuilder.DropColumn(name: "SupportStatus", table: "IntegrationMethodParameters");
            migrationBuilder.DropColumn(name: "XmlElementName", table: "IntegrationMethodParameters");
            migrationBuilder.DropColumn(name: "XsdType", table: "IntegrationMethodParameters");

            migrationBuilder.DropColumn(name: "ContractVersionId", table: "IntegrationMappingSets");
            migrationBuilder.DropColumn(name: "Revision", table: "IntegrationMappingSets");
        }
    }
}
