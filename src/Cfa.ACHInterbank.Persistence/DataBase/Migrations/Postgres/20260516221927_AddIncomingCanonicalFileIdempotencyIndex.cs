using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddIncomingCanonicalFileIdempotencyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IncomingNachaFileIngestions_FileHashSha256_FileSize",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.CreateIndex(
                name: "UX_IncomingNachaFileIngestions_FileHash_FileSize_Canonical",
                table: "IncomingNachaFileIngestions",
                columns: new[] { "FileHashSha256", "FileSize" },
                unique: true,
                filter: "\"IsReprocess\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_IncomingNachaFileIngestions_FileHash_FileSize_Canonical",
                table: "IncomingNachaFileIngestions");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaFileIngestions_FileHashSha256_FileSize",
                table: "IncomingNachaFileIngestions",
                columns: new[] { "FileHashSha256", "FileSize" });
        }
    }
}
