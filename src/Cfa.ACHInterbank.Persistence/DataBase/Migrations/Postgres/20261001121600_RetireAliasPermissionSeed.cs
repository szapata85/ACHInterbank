using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RetireAliasPermissionSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), "Consulta de alias", "CanReadAliases" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") }
                });
        }
    }
}
