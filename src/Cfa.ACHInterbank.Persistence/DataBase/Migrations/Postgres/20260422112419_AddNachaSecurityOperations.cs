using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddNachaSecurityOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NachaSecurityOperations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OperationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OperationType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: true),
                    Environment = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ExternalFileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PlainHashSha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EnvelopeHashSha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ErrorMessageSanitized = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LegacyFallbackUsed = table.Column<bool>(type: "boolean", nullable: false),
                    FailCloseApplied = table.Column<bool>(type: "boolean", nullable: false),
                    SigningCertificateThumbprintMasked = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EncryptionCertificateThumbprintMasked = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DownloadAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    DownloadAuthorizedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DownloadExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArtifactRelativePath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    ArtifactContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ArtifactSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaSecurityOperations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NachaSecurityOperations_OperationId",
                table: "NachaSecurityOperations",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NachaSecurityOperations_RequestedAtUtc_OperationType_Status",
                table: "NachaSecurityOperations",
                columns: new[] { "RequestedAtUtc", "OperationType", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NachaSecurityOperations");
        }
    }
}
