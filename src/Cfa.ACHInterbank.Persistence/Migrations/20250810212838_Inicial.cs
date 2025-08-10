using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cfa.ACHInterbank.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankHolidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClearingHouseConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    HolidayStrategy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClearingHouseConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialInstitutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialInstitutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NachaHeaders",
                columns: table => new
                {
                    NachaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PriorityCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImmediateDestination = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ImmediateOrigin = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FileCreationDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileCreationTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileIdModifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BlockingFactor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FormatCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImmediateDestinationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImmediateOriginName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaHeaders", x => x.NachaID);
                });

            migrationBuilder.CreateTable(
                name: "ClearingHouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClearingHouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClearingHouses_ClearingHouseConfigs_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouseConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BatchHeaders",
                columns: table => new
                {
                    BatchID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceClassCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DiscretionaryData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StandardEntryClassCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CompanyEntryDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescriptiveDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveEntryDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompensationDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginUserStatusCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginParticipantEntityCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BatchNumber = table.Column<int>(type: "int", nullable: false),
                    NachaID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchHeaders", x => x.BatchID);
                    table.ForeignKey(
                        name: "FK_BatchHeaders_NachaHeaders_NachaID",
                        column: x => x.NachaID,
                        principalTable: "NachaHeaders",
                        principalColumn: "NachaID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EntryDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivingParticipantEntityCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckDigit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RecipIdNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    RecipUserName = table.Column<string>(type: "nvarchar(22)", maxLength: 22, nullable: true),
                    DiscreData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddendumIndicator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SequenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NachaID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntryDetails_NachaHeaders_NachaID",
                        column: x => x.NachaID,
                        principalTable: "NachaHeaders",
                        principalColumn: "NachaID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileControls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchCount = table.Column<int>(type: "int", nullable: false),
                    BlockCount = table.Column<int>(type: "int", nullable: false),
                    EntryAddendaCount = table.Column<int>(type: "int", nullable: false),
                    EntryHash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDebitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCreditAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NachaHeaderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileControls_NachaHeaders_NachaHeaderId",
                        column: x => x.NachaHeaderId,
                        principalTable: "NachaHeaders",
                        principalColumn: "NachaID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AchCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CycleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CutoffTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    RescheduleOnHoliday = table.Column<bool>(type: "bit", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchCycles_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BatchControls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceClassCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntryAddendaCount = table.Column<int>(type: "int", nullable: false),
                    EntryHash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDebitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCreditAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OdfiIdentification = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    BatchHeaderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchControls_BatchHeaders_BatchHeaderId",
                        column: x => x.BatchHeaderId,
                        principalTable: "BatchHeaders",
                        principalColumn: "BatchID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AddendaRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentRelatedInformation = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AddendaSequenceNumber = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    EntryDetailSequenceNumber = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    EntryDetailId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddendaRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AddendaRecords_EntryDetails_EntryDetailId",
                        column: x => x.EntryDetailId,
                        principalTable: "EntryDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AchTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceInstitutionId = table.Column<int>(type: "int", nullable: false),
                    DestinationInstitutionId = table.Column<int>(type: "int", nullable: false),
                    AchCycleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchTransactions_AchCycles_AchCycleId",
                        column: x => x.AchCycleId,
                        principalTable: "AchCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchTransactions_FinancialInstitutions_DestinationInstitutionId",
                        column: x => x.DestinationInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchTransactions_FinancialInstitutions_SourceInstitutionId",
                        column: x => x.SourceInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "BankHolidays",
                columns: new[] { "Id", "Date", "Description" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Año Nuevo" },
                    { 2, new DateTime(2025, 1, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día de los Reyes Magos" },
                    { 3, new DateTime(2025, 3, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), "San José" },
                    { 4, new DateTime(2025, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "Jueves Santo" },
                    { 5, new DateTime(2025, 4, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "Viernes Santo" },
                    { 6, new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día del Trabajo" },
                    { 7, new DateTime(2025, 5, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ascensión del Señor" },
                    { 8, new DateTime(2025, 6, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Corpus Christi" },
                    { 9, new DateTime(2025, 6, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sagrado Corazón" },
                    { 10, new DateTime(2025, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día de la Independencia" },
                    { 11, new DateTime(2025, 8, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "Batalla de Boyacá" },
                    { 12, new DateTime(2025, 8, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "La Asunción" },
                    { 13, new DateTime(2025, 10, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), "Día de la Raza" },
                    { 14, new DateTime(2025, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Todos los Santos" },
                    { 15, new DateTime(2025, 11, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "Independencia de Cartagena" },
                    { 16, new DateTime(2025, 12, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "Inmaculada Concepción" },
                    { 17, new DateTime(2025, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Navidad" }
                });

            migrationBuilder.InsertData(
                table: "ClearingHouseConfigs",
                columns: new[] { "Id", "ClearingHouseId", "HolidayStrategy" },
                values: new object[] { 1, 1, "Colombian" });

            migrationBuilder.InsertData(
                table: "ClearingHouses",
                columns: new[] { "Id", "ClearingHouseId", "Code", "Name" },
                values: new object[,]
                {
                    { 1, 1, "ACHCOL", "ACH Colombia" },
                    { 2, 1, "CENIT", "CENIT" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchCycles_ClearingHouseId",
                table: "AchCycles",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_AchCycleId",
                table: "AchTransactions",
                column: "AchCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_DestinationInstitutionId",
                table: "AchTransactions",
                column: "DestinationInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_SourceInstitutionId",
                table: "AchTransactions",
                column: "SourceInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AddendaRecords_EntryDetailId",
                table: "AddendaRecords",
                column: "EntryDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchControls_BatchHeaderId",
                table: "BatchControls",
                column: "BatchHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchHeaders_NachaID",
                table: "BatchHeaders",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_ClearingHouses_ClearingHouseId",
                table: "ClearingHouses",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_EntryDetails_NachaID",
                table: "EntryDetails",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_FileControls_NachaHeaderId",
                table: "FileControls",
                column: "NachaHeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchTransactions");

            migrationBuilder.DropTable(
                name: "AddendaRecords");

            migrationBuilder.DropTable(
                name: "BankHolidays");

            migrationBuilder.DropTable(
                name: "BatchControls");

            migrationBuilder.DropTable(
                name: "FileControls");

            migrationBuilder.DropTable(
                name: "AchCycles");

            migrationBuilder.DropTable(
                name: "FinancialInstitutions");

            migrationBuilder.DropTable(
                name: "EntryDetails");

            migrationBuilder.DropTable(
                name: "BatchHeaders");

            migrationBuilder.DropTable(
                name: "ClearingHouses");

            migrationBuilder.DropTable(
                name: "NachaHeaders");

            migrationBuilder.DropTable(
                name: "ClearingHouseConfigs");
        }
    }
}
