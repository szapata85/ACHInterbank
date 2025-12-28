using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class AddClearingHouseConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.CreateTable(
                    name: "ClearingHouseConfigs",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                        ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                        HolidayStrategy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ClearingHouseConfigs", x => x.Id);
                    });
            }
            else
            {
                migrationBuilder.CreateTable(
                    name: "ClearingHouseConfigs",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                        HolidayStrategy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_ClearingHouseConfigs", x => x.Id);
                    });
            }

            migrationBuilder.CreateIndex(
                name: "IX_ClearingHouseConfigs_ClearingHouseId",
                table: "ClearingHouseConfigs",
                column: "ClearingHouseId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClearingHouses_ClearingHouseConfigs_ClearingHouseId",
                table: "ClearingHouses",
                column: "ClearingHouseId",
                principalTable: "ClearingHouseConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClearingHouses_ClearingHouseConfigs_ClearingHouseId",
                table: "ClearingHouses");

            migrationBuilder.DropTable(
                name: "ClearingHouseConfigs");
        }
    }
}
