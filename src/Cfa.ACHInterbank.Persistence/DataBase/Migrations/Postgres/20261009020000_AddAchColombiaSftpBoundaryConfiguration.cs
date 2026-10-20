using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    public partial class AddAchColombiaSftpBoundaryConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SftpHostKeyFingerprint",
                table: "AchManagedFileTransferConfigurations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SftpTimeoutSeconds",
                table: "AchManagedFileTransferConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 30);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SftpHostKeyFingerprint", table: "AchManagedFileTransferConfigurations");
            migrationBuilder.DropColumn(name: "SftpTimeoutSeconds", table: "AchManagedFileTransferConfigurations");
        }
    }
}
