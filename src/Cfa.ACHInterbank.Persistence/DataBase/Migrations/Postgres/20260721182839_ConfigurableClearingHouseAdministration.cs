using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class ConfigurableClearingHouseAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ClearingHouseSpecialDates",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "ClearingHouses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ClearingHouses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ClearingHouses",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "ClearingHouses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "NachaProfileId",
                table: "ClearingHouseConfigs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresNachaProfile",
                table: "ClearingHouseConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "ClearingHouseConfigs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Exact", "Icon", "IsActive", "Label", "MenuId", "Order", "ParentId", "Route" },
                values: new object[] { 2804, true, "account_balance", true, "Cámaras compensadoras", 1, 5, 5, "/clearing-houses" });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("c1ea0001-5b98-4d95-a100-000000000001"), "Consultar cámaras compensadoras", "ClearingHouses.View" },
                    { new Guid("c1ea0001-5b98-4d95-a100-000000000002"), "Crear cámaras compensadoras", "ClearingHouses.Create" },
                    { new Guid("c1ea0001-5b98-4d95-a100-000000000003"), "Editar cámaras compensadoras", "ClearingHouses.Update" },
                    { new Guid("c1ea0001-5b98-4d95-a100-000000000004"), "Activar o desactivar cámaras compensadoras", "ClearingHouses.ChangeStatus" },
                    { new Guid("c1ea0001-5b98-4d95-a100-000000000005"), "Administrar ciclos por cámara", "ClearingHouses.ManageCycles" },
                    { new Guid("c1ea0001-5b98-4d95-a100-000000000006"), "Administrar fechas especiales por cámara", "ClearingHouses.ManageSpecialDates" }
                });

            migrationBuilder.InsertData(
                table: "MenuItemPermissions",
                columns: new[] { "MenuItemId", "PermissionId" },
                values: new object[] { 2804, new Guid("c1ea0001-5b98-4d95-a100-000000000001") });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("c1ea0001-5b98-4d95-a100-000000000001"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("c1ea0001-5b98-4d95-a100-000000000002"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("c1ea0001-5b98-4d95-a100-000000000003"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("c1ea0001-5b98-4d95-a100-000000000004"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("c1ea0001-5b98-4d95-a100-000000000005"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("c1ea0001-5b98-4d95-a100-000000000006"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClearingHouses_IsActive_Code",
                table: "ClearingHouses",
                columns: new[] { "IsActive", "Code" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ClearingHouses_Code_Normalized",
                table: "ClearingHouses",
                sql: "\"Code\" = UPPER(TRIM(\"Code\"))");

            migrationBuilder.CreateIndex(
                name: "IX_ClearingHouseConfigs_NachaProfileId",
                table: "ClearingHouseConfigs",
                column: "NachaProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClearingHouseConfigs_CfgProfile_NachaProfileId",
                table: "ClearingHouseConfigs",
                column: "NachaProfileId",
                principalTable: "CfgProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClearingHouseConfigs_CfgProfile_NachaProfileId",
                table: "ClearingHouseConfigs");

            migrationBuilder.DropIndex(
                name: "IX_ClearingHouses_IsActive_Code",
                table: "ClearingHouses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ClearingHouses_Code_Normalized",
                table: "ClearingHouses");

            migrationBuilder.DropIndex(
                name: "IX_ClearingHouseConfigs_NachaProfileId",
                table: "ClearingHouseConfigs");

            migrationBuilder.DeleteData(
                table: "MenuItemPermissions",
                keyColumns: new[] { "MenuItemId", "PermissionId" },
                keyValues: new object[] { 2804, new Guid("c1ea0001-5b98-4d95-a100-000000000001") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("c1ea0001-5b98-4d95-a100-000000000001"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("c1ea0001-5b98-4d95-a100-000000000002"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("c1ea0001-5b98-4d95-a100-000000000003"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("c1ea0001-5b98-4d95-a100-000000000004"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("c1ea0001-5b98-4d95-a100-000000000005"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("c1ea0001-5b98-4d95-a100-000000000006"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 2804);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c1ea0001-5b98-4d95-a100-000000000001"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c1ea0001-5b98-4d95-a100-000000000002"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c1ea0001-5b98-4d95-a100-000000000003"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c1ea0001-5b98-4d95-a100-000000000004"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c1ea0001-5b98-4d95-a100-000000000005"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c1ea0001-5b98-4d95-a100-000000000006"));

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ClearingHouseSpecialDates");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ClearingHouses");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ClearingHouses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ClearingHouses");

            migrationBuilder.DropColumn(
                name: "NachaProfileId",
                table: "ClearingHouseConfigs");

            migrationBuilder.DropColumn(
                name: "RequiresNachaProfile",
                table: "ClearingHouseConfigs");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "ClearingHouseConfigs");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "ClearingHouses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
