using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddPaymentRailCapabilityRegistryPhase7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentRailCapabilityRegistry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RailCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CapabilityCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ChangeSource = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ChangeTicket = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRailCapabilityRegistry", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRailCapabilityRegistry_RailCode_CapabilityCode_IsAct~",
                table: "PaymentRailCapabilityRegistry",
                columns: new[] { "RailCode", "CapabilityCode", "IsActive", "EffectiveFromUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentRailCapabilityRegistry");
        }
    }
}
