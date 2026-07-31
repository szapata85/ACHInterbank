using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class HumanizedCertificateAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateLoadAudits_DigitalCertificateVersions_Certificat~",
                table: "CertificateLoadAudits");

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_DigitalCertificateVersions_FingerprintSha256_ClearingHouseI~";
                DROP INDEX IF EXISTS "IX_DigitalCertificateVersions_FingerprintSha256_ClearingHouseId";
                """);

            migrationBuilder.DropIndex(
                name: "UX_DCV_Active_Context",
                table: "DigitalCertificateVersions");

            migrationBuilder.AddColumn<string>(
                name: "OperationMode",
                table: "DigitalEnvelopeOperationLogs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "LIVE");

            migrationBuilder.AlterColumn<int>(
                name: "ClearingHouseId",
                table: "DigitalCertificateVersions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "FinancialInstitutionId",
                table: "DigitalCertificateVersions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedThumbprint",
                table: "DigitalCertificateVersions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RevocationReason",
                table: "DigitalCertificateVersions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokedBy",
                table: "DigitalCertificateVersions",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CertificateVersionId",
                table: "CertificateLoadAudits",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "CertificateLoadAudits",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "upload");

            migrationBuilder.AddColumn<string>(
                name: "CertificateDisplayName",
                table: "CertificateLoadAudits",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateThumbprint",
                table: "CertificateLoadAudits",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "DigitalEnvelopeOperationLogs"
                SET "OperationMode" = 'LIVE'
                WHERE "OperationMode" = '';

                UPDATE "DigitalCertificateVersions"
                SET "NormalizedThumbprint" = upper(regexp_replace(coalesce("Thumbprint", ''), '[^[:alnum:]]', '', 'g'));

                UPDATE "CertificateLoadAudits" AS audit
                SET "Action" = CASE
                        WHEN audit."LoadSource" = 'revocation' THEN 'revocation'
                        WHEN audit."LoadSource" = 'activation' THEN 'activation'
                        ELSE 'upload'
                    END,
                    "CertificateThumbprint" = version."NormalizedThumbprint",
                    "CertificateDisplayName" = certificate."DisplayName"
                FROM "DigitalCertificateVersions" AS version
                INNER JOIN "DigitalCertificates" AS certificate
                    ON certificate."Id" = version."DigitalCertificateId"
                WHERE audit."CertificateVersionId" = version."Id";

                """);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_ClearingHouseId",
                table: "DigitalCertificateVersions",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_FingerprintSha256_FinancialInsti~",
                table: "DigitalCertificateVersions",
                columns: new[] { "FingerprintSha256", "FinancialInstitutionId", "ClearingHouseId", "Environment", "Purpose", "HolderType" });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_NormalizedThumbprint",
                table: "DigitalCertificateVersions",
                column: "NormalizedThumbprint");

            migrationBuilder.CreateIndex(
                name: "UX_DCV_Active_Context",
                table: "DigitalCertificateVersions",
                columns: new[] { "FinancialInstitutionId", "ClearingHouseId", "Environment", "Purpose", "HolderType" },
                unique: true,
                filter: "\"Status\" = 2");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateLoadAudits_DigitalCertificateVersions_Certificat~",
                table: "CertificateLoadAudits",
                column: "CertificateVersionId",
                principalTable: "DigitalCertificateVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DigitalCertificateVersions_ClearingHouses_ClearingHouseId",
                table: "DigitalCertificateVersions",
                column: "ClearingHouseId",
                principalTable: "ClearingHouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DigitalCertificateVersions_FinancialInstitutions_FinancialI~",
                table: "DigitalCertificateVersions",
                column: "FinancialInstitutionId",
                principalTable: "FinancialInstitutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateLoadAudits_DigitalCertificateVersions_Certificat~",
                table: "CertificateLoadAudits");

            migrationBuilder.DropForeignKey(
                name: "FK_DigitalCertificateVersions_ClearingHouses_ClearingHouseId",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_DigitalCertificateVersions_FinancialInstitutions_FinancialI~",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropIndex(
                name: "IX_DigitalCertificateVersions_ClearingHouseId",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropIndex(
                name: "IX_DigitalCertificateVersions_FingerprintSha256_FinancialInsti~",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropIndex(
                name: "IX_DigitalCertificateVersions_NormalizedThumbprint",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropIndex(
                name: "UX_DCV_Active_Context",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropColumn(
                name: "OperationMode",
                table: "DigitalEnvelopeOperationLogs");

            migrationBuilder.DropColumn(
                name: "FinancialInstitutionId",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropColumn(
                name: "NormalizedThumbprint",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropColumn(
                name: "RevocationReason",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropColumn(
                name: "RevokedBy",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "CertificateLoadAudits");

            migrationBuilder.DropColumn(
                name: "CertificateDisplayName",
                table: "CertificateLoadAudits");

            migrationBuilder.DropColumn(
                name: "CertificateThumbprint",
                table: "CertificateLoadAudits");

            migrationBuilder.AlterColumn<int>(
                name: "ClearingHouseId",
                table: "DigitalCertificateVersions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CertificateVersionId",
                table: "CertificateLoadAudits",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_FingerprintSha256_ClearingHouseId_Environment_Purpose_HolderType",
                table: "DigitalCertificateVersions",
                columns: new[] { "FingerprintSha256", "ClearingHouseId", "Environment", "Purpose", "HolderType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DCV_Active_Context",
                table: "DigitalCertificateVersions",
                columns: new[] { "ClearingHouseId", "Environment", "Purpose", "HolderType" },
                unique: true,
                filter: "\"Status\" = 2");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateLoadAudits_DigitalCertificateVersions_Certificat~",
                table: "CertificateLoadAudits",
                column: "CertificateVersionId",
                principalTable: "DigitalCertificateVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
