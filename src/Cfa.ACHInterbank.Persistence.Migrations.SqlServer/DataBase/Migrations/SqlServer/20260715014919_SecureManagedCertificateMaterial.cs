using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class SecureManagedCertificateMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedPrivateMaterial",
                table: "DigitalCertificateVersions",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "DigitalCertificateVersions",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { new Guid("13d4e160-8be4-43eb-b69b-c9c658c2dc74"), "Administración de certificados digitales", "CanManageCertificates" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[] { new Guid("13d4e160-8be4-43eb-b69b-c9c658c2dc74"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_FingerprintSha256_ClearingHouseId_Environment_Purpose_HolderType",
                table: "DigitalCertificateVersions",
                columns: new[] { "FingerprintSha256", "ClearingHouseId", "Environment", "Purpose", "HolderType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DigitalCertificateVersions_FingerprintSha256_ClearingHouseId_Environment_Purpose_HolderType",
                table: "DigitalCertificateVersions");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("13d4e160-8be4-43eb-b69b-c9c658c2dc74"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("13d4e160-8be4-43eb-b69b-c9c658c2dc74"));

            migrationBuilder.DropColumn(
                name: "EncryptedPrivateMaterial",
                table: "DigitalCertificateVersions");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "DigitalCertificateVersions");
        }
    }
}
