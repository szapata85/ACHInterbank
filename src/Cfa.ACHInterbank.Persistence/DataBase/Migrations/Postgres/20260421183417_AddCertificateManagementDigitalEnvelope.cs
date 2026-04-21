using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddCertificateManagementDigitalEnvelope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DigitalCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeletedLogical = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DigitalCertificateVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DigitalCertificateId = table.Column<int>(type: "integer", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    HolderType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MaterialType = table.Column<int>(type: "integer", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Thumbprint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FingerprintSha256 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NotBefore = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotAfter = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HasPrivateKey = table.Column<bool>(type: "boolean", nullable: false),
                    KeyAlgorithm = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    KeySize = table.Column<int>(type: "integer", nullable: false),
                    SignatureAlgorithm = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RawPublicCertificate = table.Column<byte[]>(type: "bytea", nullable: true),
                    PrivateMaterialStorageMode = table.Column<int>(type: "integer", nullable: false),
                    SecretRef = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileRef = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByVersionId = table.Column<int>(type: "integer", nullable: true),
                    ValidationSummaryJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalCertificateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalCertificateVersions_DigitalCertificateVersions_Repla~",
                        column: x => x.ReplacedByVersionId,
                        principalTable: "DigitalCertificateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DigitalCertificateVersions_DigitalCertificates_DigitalCerti~",
                        column: x => x.DigitalCertificateId,
                        principalTable: "DigitalCertificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CertificateLoadAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CertificateVersionId = table.Column<int>(type: "integer", nullable: false),
                    LoadSource = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ValidationResult = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ValidationErrorsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    LoadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LoadedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateLoadAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateLoadAudits_DigitalCertificateVersions_Certificat~",
                        column: x => x.CertificateVersionId,
                        principalTable: "DigitalCertificateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CertificateRotationHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PreviousVersionId = table.Column<int>(type: "integer", nullable: false),
                    NewVersionId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RotatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RotatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TicketRef = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateRotationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateRotationHistories_DigitalCertificateVersions_New~",
                        column: x => x.NewVersionId,
                        principalTable: "DigitalCertificateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CertificateRotationHistories_DigitalCertificateVersions_Pre~",
                        column: x => x.PreviousVersionId,
                        principalTable: "DigitalCertificateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CertificateUsageLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CertificateVersionId = table.Column<int>(type: "integer", nullable: false),
                    OperationType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OperationId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ContextJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Result = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByProcess = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateUsageLogs_DigitalCertificateVersions_Certificate~",
                        column: x => x.CertificateVersionId,
                        principalTable: "DigitalCertificateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DigitalEnvelopeOperationLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Direction = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    CertificateVersionId = table.Column<int>(type: "integer", nullable: true),
                    FileNameIn = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    FileNameOut = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    HashPlainSha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    HashEncryptedSha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SizeBefore = table.Column<long>(type: "bigint", nullable: true),
                    SizeAfter = table.Column<long>(type: "bigint", nullable: true),
                    Result = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Actor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalEnvelopeOperationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalEnvelopeOperationLogs_DigitalCertificateVersions_Cer~",
                        column: x => x.CertificateVersionId,
                        principalTable: "DigitalCertificateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateLoadAudits_CertificateVersionId",
                table: "CertificateLoadAudits",
                column: "CertificateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateLoadAudits_LoadedAtUtc",
                table: "CertificateLoadAudits",
                column: "LoadedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRotationHistories_NewVersionId",
                table: "CertificateRotationHistories",
                column: "NewVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRotationHistories_PreviousVersionId_NewVersionId",
                table: "CertificateRotationHistories",
                columns: new[] { "PreviousVersionId", "NewVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateUsageLogs_CertificateVersionId",
                table: "CertificateUsageLogs",
                column: "CertificateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateUsageLogs_OccurredAtUtc",
                table: "CertificateUsageLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateUsageLogs_Result",
                table: "CertificateUsageLogs",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificates_Code",
                table: "DigitalCertificates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_DigitalCertificateId",
                table: "DigitalCertificateVersions",
                column: "DigitalCertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_NotAfter",
                table: "DigitalCertificateVersions",
                column: "NotAfter");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_ReplacedByVersionId",
                table: "DigitalCertificateVersions",
                column: "ReplacedByVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_SerialNumber",
                table: "DigitalCertificateVersions",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_Thumbprint",
                table: "DigitalCertificateVersions",
                column: "Thumbprint");

            migrationBuilder.CreateIndex(
                name: "UX_DCV_Active_Context",
                table: "DigitalCertificateVersions",
                columns: new[] { "ClearingHouseId", "Environment", "Purpose", "HolderType" },
                unique: true,
                filter: "\"Status\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalEnvelopeOperationLogs_CertificateVersionId",
                table: "DigitalEnvelopeOperationLogs",
                column: "CertificateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalEnvelopeOperationLogs_OccurredAtUtc_ClearingHouseId_~",
                table: "DigitalEnvelopeOperationLogs",
                columns: new[] { "OccurredAtUtc", "ClearingHouseId", "Environment", "Purpose", "Result" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateLoadAudits");

            migrationBuilder.DropTable(
                name: "CertificateRotationHistories");

            migrationBuilder.DropTable(
                name: "CertificateUsageLogs");

            migrationBuilder.DropTable(
                name: "DigitalEnvelopeOperationLogs");

            migrationBuilder.DropTable(
                name: "DigitalCertificateVersions");

            migrationBuilder.DropTable(
                name: "DigitalCertificates");
        }
    }
}
