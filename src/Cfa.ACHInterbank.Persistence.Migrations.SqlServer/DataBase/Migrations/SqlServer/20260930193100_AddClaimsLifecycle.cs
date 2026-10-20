using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddClaimsLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAt",
                table: "ClaimCases",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeadlineAt",
                table: "ClaimCases",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                table: "ClaimCases",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ClaimCases",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("UPDATE [ClaimCases] SET [Status] = 'Open' WHERE [Status] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ClaimCases",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ClaimCaseEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimCaseId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Observation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Actor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimCaseEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimCaseEvents_ClaimCases_ClaimCaseId",
                        column: x => x.ClaimCaseId,
                        principalTable: "ClaimCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO [ClaimCaseEvents] ([ClaimCaseId], [EventType], [OccurredAt], [CreatedAt], [UpdatedAt])
                SELECT [Id], 'Opened', [FiledAt], [CreatedAt], [UpdatedAt]
                FROM [ClaimCases];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ClaimCases_DeadlineAt",
                table: "ClaimCases",
                column: "DeadlineAt");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimCases_Status",
                table: "ClaimCases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimCaseEvents_ClaimCase_OccurredAt_Id",
                table: "ClaimCaseEvents",
                columns: new[] { "ClaimCaseId", "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_ClaimCaseEvents_ClaimCase_IdempotencyKey",
                table: "ClaimCaseEvents",
                columns: new[] { "ClaimCaseId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ClaimCaseEvents");

            migrationBuilder.DropIndex(name: "IX_ClaimCases_DeadlineAt", table: "ClaimCases");
            migrationBuilder.DropIndex(name: "IX_ClaimCases_Status", table: "ClaimCases");

            migrationBuilder.DropColumn(name: "ClosedAt", table: "ClaimCases");
            migrationBuilder.DropColumn(name: "DeadlineAt", table: "ClaimCases");
            migrationBuilder.DropColumn(name: "ResolutionNotes", table: "ClaimCases");
            migrationBuilder.DropColumn(name: "Status", table: "ClaimCases");
        }
    }
}
