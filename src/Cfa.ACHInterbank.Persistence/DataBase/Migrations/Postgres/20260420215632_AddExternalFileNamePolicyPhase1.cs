using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddExternalFileNamePolicyPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalFileNameRegistry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    FlowCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExternalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    InternalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    ExternalFileType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FileIdModifier = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ExternalSequence = table.Column<int>(type: "integer", nullable: true),
                    DeclaredDetailCount = table.Column<int>(type: "integer", nullable: true),
                    ActualDetailCount = table.Column<int>(type: "integer", nullable: true),
                    FileHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    ProcessingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CycleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ValidationDisposition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValidationResult = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ValidationIssuesJson = table.Column<string>(type: "text", nullable: false),
                    CorrelationEvidenceJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalFileNameRegistry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalFileSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    ScopeCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SequenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastValue = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalFileSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalFileNameValidationLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegistryId = table.Column<long>(type: "bigint", nullable: false),
                    ValidationStage = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssueCode = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    IssueMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IssuePayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalFileNameValidationLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalFileNameValidationLog_ExternalFileNameRegistry_Regi~",
                        column: x => x.RegistryId,
                        principalTable: "ExternalFileNameRegistry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFileNameRegistry_ClearingHouseId_CycleId_ExternalFi~",
                table: "ExternalFileNameRegistry",
                columns: new[] { "ClearingHouseId", "CycleId", "ExternalFileType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFileNameRegistry_ClearingHouseId_ExternalFileName_P~",
                table: "ExternalFileNameRegistry",
                columns: new[] { "ClearingHouseId", "ExternalFileName", "ProcessingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFileNameValidationLog_RegistryId_CreatedAtUtc",
                table: "ExternalFileNameValidationLog",
                columns: new[] { "RegistryId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFileSequences_ClearingHouseId_ScopeCode_SequenceDate",
                table: "ExternalFileSequences",
                columns: new[] { "ClearingHouseId", "ScopeCode", "SequenceDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalFileNameValidationLog");

            migrationBuilder.DropTable(
                name: "ExternalFileSequences");

            migrationBuilder.DropTable(
                name: "ExternalFileNameRegistry");
        }
    }
}
