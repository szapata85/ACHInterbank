using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    public partial class AddThirdPartyMasterData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThirdParties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    PersonType = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DocumentNumberNormalized = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThirdParties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThirdParties_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThirdParties_DocumentTypes_DocumentType",
                        column: x => x.DocumentType,
                        principalTable: "DocumentTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThirdParties_PersonTypes_PersonType",
                        column: x => x.PersonType,
                        principalTable: "PersonTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThirdPartyAccounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ThirdPartyId = table.Column<long>(type: "bigint", nullable: false),
                    FinancialInstitutionId = table.Column<int>(type: "integer", nullable: false),
                    AccountType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AccountNumberNormalized = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OriginSource = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThirdPartyAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThirdPartyAccounts_FinancialInstitutions_FinancialInstitutionId",
                        column: x => x.FinancialInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThirdPartyAccounts_ThirdParties_ThirdPartyId",
                        column: x => x.ThirdPartyId,
                        principalTable: "ThirdParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThirdParties_DocumentType",
                table: "ThirdParties",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_ThirdParties_PersonType",
                table: "ThirdParties",
                column: "PersonType");

            migrationBuilder.CreateIndex(
                name: "UX_ThirdParties_Customer_Document",
                table: "ThirdParties",
                columns: new[] { "CustomerId", "DocumentType", "DocumentNumberNormalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThirdPartyAccounts_FinancialInstitutionId",
                table: "ThirdPartyAccounts",
                column: "FinancialInstitutionId");

            migrationBuilder.CreateIndex(
                name: "UX_ThirdPartyAccounts_ThirdParty_Institution_Type_Number",
                table: "ThirdPartyAccounts",
                columns: new[] { "ThirdPartyId", "FinancialInstitutionId", "AccountType", "AccountNumberNormalized" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ThirdPartyAccounts");
            migrationBuilder.DropTable(name: "ThirdParties");
        }
    }
}
