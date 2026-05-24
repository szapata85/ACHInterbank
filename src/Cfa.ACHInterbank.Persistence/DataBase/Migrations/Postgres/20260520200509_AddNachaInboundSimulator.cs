using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddNachaInboundSimulator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NachaInboundSimulations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimulationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    ClearingHouseName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ScenarioType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ResponseMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ReasonCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OriginFinancialInstitutionId = table.Column<int>(type: "integer", nullable: false),
                    DestinationFinancialInstitutionId = table.Column<int>(type: "integer", nullable: false),
                    EntriesCount = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CycleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GeneratedOnly = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AutoImported = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UploadRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ExternalTransmission = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaInboundSimulations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NachaInboundSimulations_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NachaInboundSimulations_FinancialInstitutions_DestinationFi~",
                        column: x => x.DestinationFinancialInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NachaInboundSimulations_FinancialInstitutions_OriginFinanci~",
                        column: x => x.OriginFinancialInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NachaInboundSimulationEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NachaInboundSimulationId = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    TransactionId = table.Column<int>(type: "integer", nullable: true),
                    PrenotificationReference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    AccountNumberMasked = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Nature = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ExpectedStatusAfterUpload = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsSynthetic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaInboundSimulationEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NachaInboundSimulationEntries_NachaInboundSimulations_Nacha~",
                        column: x => x.NachaInboundSimulationId,
                        principalTable: "NachaInboundSimulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Exact", "Icon", "IsActive", "Label", "MenuId", "Order", "ParentId", "Route" },
                values: new object[] { 33, false, "science", true, "UAT / Simuladores", 1, 11, null, "/uat" });

            migrationBuilder.InsertData(
                table: "MenuItemPermissions",
                columns: new[] { "MenuItemId", "PermissionId" },
                values: new object[,]
                {
                    { 33, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 33, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") }
                });

            migrationBuilder.InsertData(
                table: "MenuItemRoles",
                columns: new[] { "MenuItemId", "RoleId" },
                values: new object[,]
                {
                    { 33, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 33, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") }
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Exact", "Icon", "IsActive", "Label", "MenuId", "Order", "ParentId", "Route" },
                values: new object[] { 34, true, "file_download", true, "Simulador NACHA-M Entrada", 1, 1, 33, "/uat/nacha-inbound-simulator" });

            migrationBuilder.InsertData(
                table: "MenuItemPermissions",
                columns: new[] { "MenuItemId", "PermissionId" },
                values: new object[,]
                {
                    { 34, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 34, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") }
                });

            migrationBuilder.InsertData(
                table: "MenuItemRoles",
                columns: new[] { "MenuItemId", "RoleId" },
                values: new object[,]
                {
                    { 34, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 34, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_NachaInboundSimulationEntries_NachaInboundSimulationId",
                table: "NachaInboundSimulationEntries",
                column: "NachaInboundSimulationId");

            migrationBuilder.CreateIndex(
                name: "IX_NachaInboundSimulations_ClearingHouseId_ScenarioType",
                table: "NachaInboundSimulations",
                columns: new[] { "ClearingHouseId", "ScenarioType" });

            migrationBuilder.CreateIndex(
                name: "IX_NachaInboundSimulations_CreatedAt",
                table: "NachaInboundSimulations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NachaInboundSimulations_DestinationFinancialInstitutionId",
                table: "NachaInboundSimulations",
                column: "DestinationFinancialInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_NachaInboundSimulations_FileName",
                table: "NachaInboundSimulations",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_NachaInboundSimulations_OriginFinancialInstitutionId",
                table: "NachaInboundSimulations",
                column: "OriginFinancialInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_NachaInboundSimulations_SimulationId",
                table: "NachaInboundSimulations",
                column: "SimulationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NachaInboundSimulationEntries");

            migrationBuilder.DropTable(
                name: "NachaInboundSimulations");

            migrationBuilder.DeleteData(
                table: "MenuItemPermissions",
                keyColumns: new[] { "MenuItemId", "PermissionId" },
                keyValues: new object[] { 33, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") });

            migrationBuilder.DeleteData(
                table: "MenuItemPermissions",
                keyColumns: new[] { "MenuItemId", "PermissionId" },
                keyValues: new object[] { 33, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") });

            migrationBuilder.DeleteData(
                table: "MenuItemPermissions",
                keyColumns: new[] { "MenuItemId", "PermissionId" },
                keyValues: new object[] { 34, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") });

            migrationBuilder.DeleteData(
                table: "MenuItemPermissions",
                keyColumns: new[] { "MenuItemId", "PermissionId" },
                keyValues: new object[] { 34, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") });

            migrationBuilder.DeleteData(
                table: "MenuItemRoles",
                keyColumns: new[] { "MenuItemId", "RoleId" },
                keyValues: new object[] { 33, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "MenuItemRoles",
                keyColumns: new[] { "MenuItemId", "RoleId" },
                keyValues: new object[] { 33, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") });

            migrationBuilder.DeleteData(
                table: "MenuItemRoles",
                keyColumns: new[] { "MenuItemId", "RoleId" },
                keyValues: new object[] { 34, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "MenuItemRoles",
                keyColumns: new[] { "MenuItemId", "RoleId" },
                keyValues: new object[] { 34, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") });

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 33);
        }
    }
}
