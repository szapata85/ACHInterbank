using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.CreateTable(
                    name: "BrandingSettings",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                        PublicLogo = table.Column<string>(type: "text", nullable: true),
                        PrivateLogo = table.Column<string>(type: "text", nullable: true),
                        PublicBackground = table.Column<string>(type: "text", nullable: true),
                        PrivateBackground = table.Column<string>(type: "text", nullable: true),
                        SidebarBackground = table.Column<string>(type: "text", nullable: true),
                        ButtonColor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                        CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                        UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_BrandingSettings", x => x.Id);
                    });
            }
            else
            {
                migrationBuilder.CreateTable(
                    name: "BrandingSettings",
                    columns: table => new
                    {
                        Id = table.Column<int>(type: "int", nullable: false)
                            .Annotation("SqlServer:Identity", "1, 1"),
                        PublicLogo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        PrivateLogo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        PublicBackground = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        PrivateBackground = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        SidebarBackground = table.Column<string>(type: "nvarchar(max)", nullable: true),
                        ButtonColor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                        CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                        UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_BrandingSettings", x => x.Id);
                    });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandingSettings");
        }
    }
}
