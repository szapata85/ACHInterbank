using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class SyncAchDbContext20261116 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AchTransactions_SourceInstitutionId",
                table: "AchTransactions");

            migrationBuilder.AlterColumn<bool>(
                name: "RequiresReconciliation",
                table: "IncomingNachaDispatchQueue",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "RequiresReconciliation",
                table: "IncomingNachaDispatchQueue",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_SourceInstitutionId",
                table: "AchTransactions",
                column: "SourceInstitutionId");
        }
    }
}
