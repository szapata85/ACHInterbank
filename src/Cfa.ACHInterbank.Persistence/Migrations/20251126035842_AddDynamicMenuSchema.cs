using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cfa.ACHInterbank.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicMenuSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuId = table.Column<int>(type: "int", nullable: true),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Exact = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_MenuItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItems_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemPermissions",
                columns: table => new
                {
                    MenuItemId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemPermissions", x => new { x.MenuItemId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_MenuItemPermissions_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuItemPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemRoles",
                columns: table => new
                {
                    MenuItemId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemRoles", x => new { x.MenuItemId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_MenuItemRoles_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuItemRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[] { 1, "Menú principal", true, "Principal" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7"),
                column: "Name",
                value: "CanReadAch");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a"),
                column: "Name",
                value: "CanManageAch");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), "Consulta de alias", "CanReadAliases" },
                    { new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf"), "Gestión de usuarios", "CanManageUsers" },
                    { new Guid("dd0e54be-b6df-4ab3-8783-0f72b6e774a2"), "Consulta de catálogos", "CanReadCatalogs" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[] { new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a51746c2-0710-4d79-97b1-5b4368326f56"),
                column: "Name",
                value: "ACH.Operator");

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Exact", "Icon", "IsActive", "Label", "MenuId", "Order", "ParentId", "Route" },
                values: new object[,]
                {
                    { 1, true, "dashboard", true, "Dashboard", 1, 1, null, "/dashboard" },
                    { 2, true, "group", true, "Usuarios", 1, 2, null, "/users" },
                    { 3, true, "key", true, "Alias", 1, 3, null, "/aliases" },
                    { 4, true, "schedule", true, "Ciclos ACH", 1, 4, null, "/ach-cycles" },
                    { 5, true, "inventory", true, "Catálogos", 1, 5, null, "/catalogs" },
                    { 6, false, "swap_horiz", true, "Transacciones", 1, 6, null, "/transactions" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("dd0e54be-b6df-4ab3-8783-0f72b6e774a2"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { new Guid("dd0e54be-b6df-4ab3-8783-0f72b6e774a2"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") }
                });

            migrationBuilder.InsertData(
                table: "MenuItemPermissions",
                columns: new[] { "MenuItemId", "PermissionId" },
                values: new object[,]
                {
                    { 2, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") },
                    { 3, new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7") },
                    { 4, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 5, new Guid("dd0e54be-b6df-4ab3-8783-0f72b6e774a2") },
                    { 6, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 6, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") }
                });

            migrationBuilder.InsertData(
                table: "MenuItemRoles",
                columns: new[] { "MenuItemId", "RoleId" },
                values: new object[,]
                {
                    { 2, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 6, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 6, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") }
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Exact", "Icon", "IsActive", "Label", "MenuId", "Order", "ParentId", "Route" },
                values: new object[] { 7, true, "note_add", true, "Crear transacción", 1, 1, 6, "/transactions/create" });

            migrationBuilder.InsertData(
                table: "MenuItemPermissions",
                columns: new[] { "MenuItemId", "PermissionId" },
                values: new object[,]
                {
                    { 7, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 7, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") }
                });

            migrationBuilder.InsertData(
                table: "MenuItemRoles",
                columns: new[] { "MenuItemId", "RoleId" },
                values: new object[,]
                {
                    { 7, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 7, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPermissions_PermissionId",
                table: "MenuItemPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemRoles_RoleId",
                table: "MenuItemRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuId",
                table: "MenuItems",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_ParentId",
                table: "MenuItems",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuItemPermissions");

            migrationBuilder.DropTable(
                name: "MenuItemRoles");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("dd0e54be-b6df-4ab3-8783-0f72b6e774a2"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("dd0e54be-b6df-4ab3-8783-0f72b6e774a2"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") });

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("dd0e54be-b6df-4ab3-8783-0f72b6e774a2"));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7"),
                column: "Name",
                value: "ach.read");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a"),
                column: "Name",
                value: "ach.manage");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a51746c2-0710-4d79-97b1-5b4368326f56"),
                column: "Name",
                value: "Operator");
        }
    }
}
