using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class HumanizedCertificateAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateLoadAudits_DigitalCertificateVersions_CertificateVersionId",
                table: "CertificateLoadAudits");

            migrationBuilder.DropIndex(
                name: "IX_DigitalCertificateVersions_FingerprintSha256_ClearingHouseId_Environment_Purpose_HolderType",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropIndex(
                name: "UX_DCV_Active_Context",
                table: "DigitalCertificateVersions");

            migrationBuilder.AddColumn<string>(
                name: "OperationMode",
                table: "DigitalEnvelopeOperationLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "LIVE");

            migrationBuilder.AlterColumn<int>(
                name: "ClearingHouseId",
                table: "DigitalCertificateVersions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "FinancialInstitutionId",
                table: "DigitalCertificateVersions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedThumbprint",
                table: "DigitalCertificateVersions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RevocationReason",
                table: "DigitalCertificateVersions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokedBy",
                table: "DigitalCertificateVersions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CertificateVersionId",
                table: "CertificateLoadAudits",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "CertificateLoadAudits",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "upload");

            migrationBuilder.AddColumn<string>(
                name: "CertificateDisplayName",
                table: "CertificateLoadAudits",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateThumbprint",
                table: "CertificateLoadAudits",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [DigitalEnvelopeOperationLogs]
                SET [OperationMode] = 'LIVE'
                WHERE [OperationMode] = '';

                UPDATE [DigitalCertificateVersions]
                SET [NormalizedThumbprint] = UPPER(REPLACE(REPLACE(COALESCE([Thumbprint], ''), ' ', ''), ':', ''));

                UPDATE audit
                SET [Action] = CASE
                        WHEN audit.[LoadSource] = 'revocation' THEN 'revocation'
                        WHEN audit.[LoadSource] = 'activation' THEN 'activation'
                        ELSE 'upload'
                    END,
                    [CertificateThumbprint] = version.[NormalizedThumbprint],
                    [CertificateDisplayName] = certificate.[DisplayName]
                FROM [CertificateLoadAudits] AS audit
                INNER JOIN [DigitalCertificateVersions] AS version
                    ON version.[Id] = audit.[CertificateVersionId]
                INNER JOIN [DigitalCertificates] AS certificate
                    ON certificate.[Id] = version.[DigitalCertificateId];

                """);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_ClearingHouseId",
                table: "DigitalCertificateVersions",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_FingerprintSha256_FinancialInstitutionId_ClearingHouseId_Environment_Purpose_HolderType",
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
                filter: "[Status] = 2");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateLoadAudits_DigitalCertificateVersions_CertificateVersionId",
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
                name: "FK_DigitalCertificateVersions_FinancialInstitutions_FinancialInstitutionId",
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
                name: "FK_CertificateLoadAudits_DigitalCertificateVersions_CertificateVersionId",
                table: "CertificateLoadAudits");

            migrationBuilder.DropForeignKey(
                name: "FK_DigitalCertificateVersions_ClearingHouses_ClearingHouseId",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_DigitalCertificateVersions_FinancialInstitutions_FinancialInstitutionId",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropIndex(
                name: "IX_DigitalCertificateVersions_ClearingHouseId",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropIndex(
                name: "IX_DigitalCertificateVersions_FingerprintSha256_FinancialInstitutionId_ClearingHouseId_Environment_Purpose_HolderType",
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
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CertificateVersionId",
                table: "CertificateLoadAudits",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
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
                filter: "[Status] = 2");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateLoadAudits_DigitalCertificateVersions_CertificateVersionId",
                table: "CertificateLoadAudits",
                column: "CertificateVersionId",
                principalTable: "DigitalCertificateVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
