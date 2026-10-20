using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class HardenExternalFileNameReservationsExecution3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GenerationReservationId",
                table: "ExternalFileNameRegistry",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExternalFileNameReservations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    ScopeCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    OperationalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IdempotencyKeyHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    RequestFingerprintHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    FileIdModifier = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    ExternalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReservedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalFileNameReservations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ExternalFileNameRegistry_GenerationReservation",
                table: "ExternalFileNameRegistry",
                column: "GenerationReservationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFileNameReservations_OperationalDate_Status",
                table: "ExternalFileNameReservations",
                columns: new[] { "OperationalDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_ExternalFileNameReservations_Idempotency",
                table: "ExternalFileNameReservations",
                columns: new[] { "ClearingHouseId", "IdempotencyKeyHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ExternalFileNameReservations_Sequence",
                table: "ExternalFileNameReservations",
                columns: new[] { "ClearingHouseId", "ScopeCode", "OperationalDate", "Sequence" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalFileNameRegistry_ExternalFileNameReservations_Gener~",
                table: "ExternalFileNameRegistry",
                column: "GenerationReservationId",
                principalTable: "ExternalFileNameReservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExternalFileNameRegistry_ExternalFileNameReservations_Gener~",
                table: "ExternalFileNameRegistry");

            migrationBuilder.DropTable(
                name: "ExternalFileNameReservations");

            migrationBuilder.DropIndex(
                name: "UX_ExternalFileNameRegistry_GenerationReservation",
                table: "ExternalFileNameRegistry");

            migrationBuilder.DropColumn(
                name: "GenerationReservationId",
                table: "ExternalFileNameRegistry");
        }
    }
}
