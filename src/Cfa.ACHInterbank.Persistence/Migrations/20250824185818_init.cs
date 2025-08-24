using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cfa.ACHInterbank.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
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
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                    HolidayStrategy = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    NachaID = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PriorityCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImmediateDestination = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ImmediateOrigin = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    FileCreationDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileCreationTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileIdModifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordSize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BlockingFactor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormatCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImmediateDestinationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImmediateOriginName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaHeaders", x => x.NachaID);
                });

            migrationBuilder.CreateTable(
                name: "TaskDefinition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CalendarPolicy = table.Column<int>(type: "int", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConcurrencyPolicy = table.Column<int>(type: "int", nullable: false),
                    RetryOnFailure = table.Column<bool>(type: "bit", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: true),
                    RetryBackoffSeconds = table.Column<int>(type: "int", nullable: false),
                    PeriodicityType = table.Column<int>(type: "int", nullable: false),
                    N = table.Column<int>(type: "int", nullable: true),
                    Minute = table.Column<int>(type: "int", nullable: true),
                    TimeOfDay = table.Column<TimeOnly>(type: "time", nullable: true),
                    WeeklyDay = table.Column<int>(type: "int", nullable: true),
                    MonthDay = table.Column<int>(type: "int", nullable: true),
                    CronExpression = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDefinition", x => x.Id);
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
                name: "AddendaRecords",
                columns: table => new
                {
                    AddendaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeTypeAddendumRecord = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdUserOrig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurposeOfTransaction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceOrAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InfofromOriginator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddendumSequence = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    EntryDetailSequenceNumber = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    NachaID = table.Column<string>(type: "nvarchar(64)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddendaRecords", x => x.AddendaID);
                    table.ForeignKey(
                        name: "FK_AddendaRecords_NachaHeaders_NachaID",
                        column: x => x.NachaID,
                        principalTable: "NachaHeaders",
                        principalColumn: "NachaID");
                });

            migrationBuilder.CreateTable(
                name: "BatchControls",
                columns: table => new
                {
                    BatchControlID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchTranClassCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntryAddendaCount = table.Column<int>(type: "int", nullable: true),
                    TotalEntry = table.Column<int>(type: "int", nullable: true),
                    TotalDebitAmount = table.Column<decimal>(type: "money", nullable: false),
                    TotalCreditAmount = table.Column<decimal>(type: "money", nullable: false),
                    IdUserOrig = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CodAutMessage = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    IdOrigEntity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NachaID = table.Column<string>(type: "nvarchar(64)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchControls", x => x.BatchControlID);
                    table.ForeignKey(
                        name: "FK_BatchControls_NachaHeaders_NachaID",
                        column: x => x.NachaID,
                        principalTable: "NachaHeaders",
                        principalColumn: "NachaID");
                });

            migrationBuilder.CreateTable(
                name: "BatchHeaders",
                columns: table => new
                {
                    BatchID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceClassCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    DiscretionaryData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    StandardEntryClassCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    CompanyEntryDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptiveDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EffectiveEntryDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompensationDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginUserStatusCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginParticipantEntityCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BatchNumber = table.Column<int>(type: "int", nullable: false),
                    NachaID = table.Column<string>(type: "nvarchar(64)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchHeaders", x => x.BatchID);
                    table.ForeignKey(
                        name: "FK_BatchHeaders_NachaHeaders_NachaID",
                        column: x => x.NachaID,
                        principalTable: "NachaHeaders",
                        principalColumn: "NachaID");
                });

            migrationBuilder.CreateTable(
                name: "EntryDetails",
                columns: table => new
                {
                    EntryDetailID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivingParticipantEntityCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckDigit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: true),
                    Amount = table.Column<decimal>(type: "money", nullable: true),
                    RecipIdNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    RecipUserName = table.Column<string>(type: "nvarchar(22)", maxLength: 22, nullable: true),
                    DiscreData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddendumIndicator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SequenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NachaID = table.Column<string>(type: "nvarchar(64)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntryDetails", x => x.EntryDetailID);
                    table.ForeignKey(
                        name: "FK_EntryDetails_NachaHeaders_NachaID",
                        column: x => x.NachaID,
                        principalTable: "NachaHeaders",
                        principalColumn: "NachaID");
                });

            migrationBuilder.CreateTable(
                name: "FileControls",
                columns: table => new
                {
                    FileControlID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchCount = table.Column<int>(type: "int", nullable: false),
                    BlockCount = table.Column<int>(type: "int", nullable: false),
                    EntryAddendaCount = table.Column<int>(type: "int", nullable: false),
                    TotalControl = table.Column<decimal>(type: "money", nullable: false),
                    TotalDebitAmount = table.Column<decimal>(type: "money", nullable: false),
                    TotalCreditAmount = table.Column<decimal>(type: "money", nullable: false),
                    NachaID = table.Column<string>(type: "nvarchar(64)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileControls", x => x.FileControlID);
                    table.ForeignKey(
                        name: "FK_FileControls_NachaHeaders_NachaID",
                        column: x => x.NachaID,
                        principalTable: "NachaHeaders",
                        principalColumn: "NachaID");
                });

            migrationBuilder.CreateTable(
                name: "TaskExecutionLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskDefinitionId = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Output = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskExecutionLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskExecutionLog_TaskDefinition_TaskDefinitionId",
                        column: x => x.TaskDefinitionId,
                        principalTable: "TaskDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskParameters_TaskDefinition_TaskDefinitionId",
                        column: x => x.TaskDefinitionId,
                        principalTable: "TaskDefinition",
                        principalColumn: "Id",
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
                columns: new[] { "Id", "CountryCode", "Date", "Description" },
                values: new object[,]
                {
                    { 1, "CO", new DateOnly(2025, 1, 1), "Año Nuevo" },
                    { 2, "CO", new DateOnly(2025, 1, 6), "Día de los Reyes Magos" },
                    { 3, "CO", new DateOnly(2025, 3, 24), "San José" },
                    { 4, "CO", new DateOnly(2025, 4, 17), "Jueves Santo" },
                    { 5, "CO", new DateOnly(2025, 4, 18), "Viernes Santo" },
                    { 6, "CO", new DateOnly(2025, 5, 1), "Día del Trabajo" },
                    { 7, "CO", new DateOnly(2025, 5, 26), "Ascensión del Señor" },
                    { 8, "CO", new DateOnly(2025, 6, 16), "Corpus Christi" },
                    { 9, "CO", new DateOnly(2025, 6, 23), "Sagrado Corazón" },
                    { 10, "CO", new DateOnly(2025, 7, 20), "Día de la Independencia" },
                    { 11, "CO", new DateOnly(2025, 8, 7), "Batalla de Boyacá" },
                    { 12, "CO", new DateOnly(2025, 8, 18), "La Asunción" },
                    { 13, "CO", new DateOnly(2025, 10, 13), "Día de la Raza" },
                    { 14, "CO", new DateOnly(2025, 11, 3), "Todos los Santos" },
                    { 15, "CO", new DateOnly(2025, 11, 17), "Independencia de Cartagena" },
                    { 16, "CO", new DateOnly(2025, 12, 8), "Inmaculada Concepción" },
                    { 17, "CO", new DateOnly(2025, 12, 25), "Navidad" }
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
                name: "IX_AddendaRecords_NachaID",
                table: "AddendaRecords",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_BatchControls_NachaID",
                table: "BatchControls",
                column: "NachaID");

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
                name: "IX_FileControls_NachaID",
                table: "FileControls",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDefinition_Code",
                table: "TaskDefinition",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskExecutionLog_TaskDefinitionId",
                table: "TaskExecutionLog",
                column: "TaskDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskParameters_TaskDefinitionId_Key",
                table: "TaskParameters",
                columns: new[] { "TaskDefinitionId", "Key" },
                unique: true);
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
                name: "BatchHeaders");

            migrationBuilder.DropTable(
                name: "EntryDetails");

            migrationBuilder.DropTable(
                name: "FileControls");

            migrationBuilder.DropTable(
                name: "TaskExecutionLog");

            migrationBuilder.DropTable(
                name: "TaskParameters");

            migrationBuilder.DropTable(
                name: "AchCycles");

            migrationBuilder.DropTable(
                name: "FinancialInstitutions");

            migrationBuilder.DropTable(
                name: "NachaHeaders");

            migrationBuilder.DropTable(
                name: "TaskDefinition");

            migrationBuilder.DropTable(
                name: "ClearingHouses");

            migrationBuilder.DropTable(
                name: "ClearingHouseConfigs");
        }
    }
}
