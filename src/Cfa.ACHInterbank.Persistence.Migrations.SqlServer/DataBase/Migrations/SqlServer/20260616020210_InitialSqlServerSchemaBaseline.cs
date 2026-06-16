using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class InitialSqlServerSchemaBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchFileRejectionCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AppliesToStage = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsRetryable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchFileRejectionCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchPrenotificationPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAddenda = table.Column<bool>(type: "bit", nullable: false),
                    BlocksMonetaryTransactionIfMissing = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchPrenotificationPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoRespuesta = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IdTransaccion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CodigoCamaraCompensacion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodigoEntidadOrigen = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CodigoEntidadDestino = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CodigoEstadoExterno = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodigoCausalExterna = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdEstadoInterno = table.Column<int>(type: "int", nullable: true),
                    IdEstadoServicioExterno = table.Column<int>(type: "int", nullable: true),
                    EstadoInternoNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CausalNormalizada = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DescripcionCausal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IdTransaccionServicioExterno = table.Column<int>(type: "int", nullable: false),
                    HashIdempotencia = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EstadoProcesamiento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MotivoNoHomologacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PermiteNotificacion = table.Column<bool>(type: "bit", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaRecepcion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchResponses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchResponseStatusMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoCamaraCompensacion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TipoRespuesta = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodigoEstadoExterno = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodigoCausalExterna = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdEstadoInterno = table.Column<int>(type: "int", nullable: false),
                    IdEstadoServicioExterno = table.Column<int>(type: "int", nullable: false),
                    EstadoInternoNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CausalNormalizada = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DescripcionCausalNormalizada = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RequiereCausal = table.Column<bool>(type: "bit", nullable: false),
                    PermiteNotificacion = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaInicioVigencia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFinVigencia = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchResponseStatusMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchReturnOfReturnGeneratedFileAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedFlowCount = table.Column<int>(type: "int", nullable: false),
                    ContentLength = table.Column<int>(type: "int", nullable: false),
                    ContentSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchReturnOfReturnGeneratedFileAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchTransactionTypePolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PriorityOrder = table.Column<int>(type: "int", nullable: false),
                    IsMonetary = table.Column<bool>(type: "bit", nullable: false),
                    RequiresPrenotification = table.Column<bool>(type: "bit", nullable: false),
                    CanBeReturned = table.Column<bool>(type: "bit", nullable: false),
                    CanBeReturnedAgain = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchTransactionTypePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AddressTypes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddressTypes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    LoggedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthLog", x => x.Id);
                });

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
                name: "BatchNumberSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OriginatingDfi = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ProcessingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PolicyCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastAssignedValue = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BrandingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicLogo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrivateLogo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublicBackground = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrivateBackground = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SidebarBackground = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ButtonColor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandingSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BulkIngestionBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchReference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ClientRequestId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TotalRecords = table.Column<int>(type: "int", nullable: false),
                    TotalValid = table.Column<int>(type: "int", nullable: false),
                    TotalInvalid = table.Column<int>(type: "int", nullable: false),
                    TotalProcessed = table.Column<int>(type: "int", nullable: false),
                    TotalSucceeded = table.Column<int>(type: "int", nullable: false),
                    TotalFailed = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SummaryErrorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QueuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingFinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastJobId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LastJobMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkIngestionBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatClearingHouse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatClearingHouse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatConfigStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false),
                    IsPublishable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatConfigStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatDataSourceType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NameEs = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatDataSourceType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatDirection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameEs = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatDirection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatRecordCode",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    NameEs = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsMandatoryBase = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatRecordCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatRuleType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NameEs = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatRuleType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CfgRuleSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleSetCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEs = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Scope = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfgRuleSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClearingHouseConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    HolidayStrategy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClearingHouseConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyEntryDescription",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Term = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StandardEntryClassCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyEntryDescription", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DigitalCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeletedLogical = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DigitalEnvelopeCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    RawData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HasPrivateKey = table.Column<bool>(type: "bit", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Issuer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Thumbprint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NotBefore = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotAfter = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalEnvelopeCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "EmailTypes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTypes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "ExternalFileNameRegistry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    FlowCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExternalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    InternalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ExternalFileType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FileIdModifier = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    ExternalSequence = table.Column<int>(type: "int", nullable: true),
                    DeclaredDetailCount = table.Column<int>(type: "int", nullable: true),
                    ActualDetailCount = table.Column<int>(type: "int", nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    ProcessingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CycleId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ValidationDisposition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValidationResult = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ValidationIssuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationEvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalFileNameRegistry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalFileSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    ScopeCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SequenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastValue = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalFileSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GenderTypes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenderTypes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "IncomingNachaFileIngestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileHashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ReceivedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IngestionStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CycleResolutionStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ParsingStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DetectedClearingHouseId = table.Column<int>(type: "int", nullable: true),
                    ResolvedClearingHouseId = table.Column<int>(type: "int", nullable: true),
                    OperationalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAchCycleId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ResolutionMode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ResolutionConfidence = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ResolutionEvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawStorageReference = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ParentIngestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsReprocess = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    WarningsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomingNachaFileIngestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncomingNachaFileIngestions_IncomingNachaFileIngestions_ParentIngestionId",
                        column: x => x.ParentIngestionId,
                        principalTable: "IncomingNachaFileIngestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationMappingTraces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OperationKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MappingPurpose = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MappingDirection = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TransactionId = table.Column<int>(type: "int", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    MappingSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MappingVersion = table.Column<int>(type: "int", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DryRun = table.Column<bool>(type: "bit", nullable: false),
                    ExternalTransmission = table.Column<bool>(type: "bit", nullable: false),
                    MonetaryMovementCreated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationMappingTraces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoapClientCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginLockoutSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaxFailedAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginLockoutSettings", x => x.Id);
                });

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
                name: "NachaFileIdentifierMap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaFileIdentifierMap", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NachaRecordDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FilterKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaRecordDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NachaRecordLayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalLength = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaRecordLayouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NachaSecurityOperations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ExternalFileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PlainHashSha256 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EnvelopeHashSha256 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ErrorMessageSanitized = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LegacyFallbackUsed = table.Column<bool>(type: "bit", nullable: false),
                    FailCloseApplied = table.Column<bool>(type: "bit", nullable: false),
                    SigningCertificateThumbprintMasked = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EncryptionCertificateThumbprintMasked = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DownloadAvailable = table.Column<bool>(type: "bit", nullable: false),
                    DownloadAuthorizedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DownloadExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArtifactRelativePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ArtifactContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ArtifactSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaSecurityOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NavigationLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Route = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    VisitedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavigationLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordRuleSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinLength = table.Column<int>(type: "int", nullable: false),
                    MinUppercase = table.Column<int>(type: "int", nullable: false),
                    MinNumbers = table.Column<int>(type: "int", nullable: false),
                    MinSpecial = table.Column<int>(type: "int", nullable: false),
                    MaxSpecial = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordRuleSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRailCapabilityRegistry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RailCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CapabilityCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ChangeSource = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ChangeTicket = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRailCapabilityRegistry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonTypes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonTypes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "PhoneTypes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneTypes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "ReturnReasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    IsForReturn = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnReasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SoapIntegrationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WscfaachMappingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WsAxonRespuestaTransaccionesMappingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoapIntegrationSettings", x => x.Id);
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
                    TimeOfDayTicks = table.Column<long>(type: "bigint", nullable: true),
                    WeeklyDay = table.Column<int>(type: "int", nullable: true),
                    MonthDay = table.Column<int>(type: "int", nullable: true),
                    CronExpression = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDefinition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionCodes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchResponseNotificationAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroIntento = table.Column<int>(type: "int", nullable: false),
                    EstadoNotificacion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IdCanal = table.Column<int>(type: "int", nullable: false),
                    NombreCanal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdTransaccion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdEstado = table.Column<int>(type: "int", nullable: false),
                    Causal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdTransaccionServicioExterno = table.Column<int>(type: "int", nullable: false),
                    DescripcionCausal = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RequestPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsePayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExisteError = table.Column<bool>(type: "bit", nullable: true),
                    CodigoError = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DescripcionError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorTecnico = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchResponseNotificationAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchResponseNotificationAttempts_AchResponses_AchResponseId",
                        column: x => x.AchResponseId,
                        principalTable: "AchResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BulkIngestionAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TriggeredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    JobId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalProcessed = table.Column<int>(type: "int", nullable: false),
                    TotalSucceeded = table.Column<int>(type: "int", nullable: false),
                    TotalFailed = table.Column<int>(type: "int", nullable: false),
                    ResultMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkIngestionAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkIngestionAttempts_BulkIngestionBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "BulkIngestionBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BulkIngestionItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemIndex = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TransactionId = table.Column<int>(type: "int", nullable: true),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NormalizedPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkIngestionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkIngestionItems_BulkIngestionBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "BulkIngestionBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CatServiceClass",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameEs = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatServiceClass", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatServiceClass_CatClearingHouse_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "CatClearingHouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CfgFieldSourceDefinition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataSourceTypeId = table.Column<int>(type: "int", nullable: false),
                    ConstantValue = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PropertyPath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SqlObjectName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ExpressionDsl = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExternalCatalogCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FallbackPolicyJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfgFieldSourceDefinition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CfgFieldSourceDefinition_CatDataSourceType_DataSourceTypeId",
                        column: x => x.DataSourceTypeId,
                        principalTable: "CatDataSourceType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CatFlowType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NameEs = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DirectionDefaultId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatFlowType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatFlowType_CatDirection_DirectionDefaultId",
                        column: x => x.DirectionDefaultId,
                        principalTable: "CatDirection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CfgRuleSetRule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleSetId = table.Column<int>(type: "int", nullable: false),
                    RuleTypeId = table.Column<int>(type: "int", nullable: false),
                    RuleCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConditionDsl = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RuleConfigJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ErrorMessageEs = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfgRuleSetRule", x => x.Id);
                    table.CheckConstraint("CK_CfgRuleSetRule_Order_NonNegative", "\"Order\" >= 0");
                    table.ForeignKey(
                        name: "FK_CfgRuleSetRule_CatRuleType_RuleTypeId",
                        column: x => x.RuleTypeId,
                        principalTable: "CatRuleType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfgRuleSetRule_CfgRuleSet_RuleSetId",
                        column: x => x.RuleSetId,
                        principalTable: "CfgRuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClearingHouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DigitalCertificateVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DigitalCertificateId = table.Column<int>(type: "int", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    HolderType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MaterialType = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Issuer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Thumbprint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FingerprintSha256 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NotBefore = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotAfter = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HasPrivateKey = table.Column<bool>(type: "bit", nullable: false),
                    KeyAlgorithm = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    KeySize = table.Column<int>(type: "int", nullable: false),
                    SignatureAlgorithm = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RawPublicCertificate = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PrivateMaterialStorageMode = table.Column<int>(type: "int", nullable: false),
                    SecretRef = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileRef = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByVersionId = table.Column<int>(type: "int", nullable: true),
                    ValidationSummaryJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalCertificateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalCertificateVersions_DigitalCertificateVersions_ReplacedByVersionId",
                        column: x => x.ReplacedByVersionId,
                        principalTable: "DigitalCertificateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DigitalCertificateVersions_DigitalCertificates_DigitalCertificateId",
                        column: x => x.DigitalCertificateId,
                        principalTable: "DigitalCertificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalFileNameValidationLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistryId = table.Column<long>(type: "bigint", nullable: false),
                    ValidationStage = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RuleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssueCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    IssueMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IssuePayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalFileNameValidationLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalFileNameValidationLog_ExternalFileNameRegistry_RegistryId",
                        column: x => x.RegistryId,
                        principalTable: "ExternalFileNameRegistry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncomingNachaFileProcessingResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncomingNachaFileIngestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalBatches = table.Column<int>(type: "int", nullable: false),
                    TotalEntries = table.Column<int>(type: "int", nullable: false),
                    TotalAddendas = table.Column<int>(type: "int", nullable: false),
                    ValidCount = table.Column<int>(type: "int", nullable: false),
                    InvalidCount = table.Column<int>(type: "int", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    OutcomeStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FailureStage = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ParserWarningsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParserErrorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsReprocessable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomingNachaFileProcessingResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncomingNachaFileProcessingResults_IncomingNachaFileIngestions_IncomingNachaFileIngestionId",
                        column: x => x.IncomingNachaFileIngestionId,
                        principalTable: "IncomingNachaFileIngestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncomingNachaProcessingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncomingNachaFileIngestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryDetailId = table.Column<int>(type: "int", nullable: true),
                    AddendaRecordId = table.Column<int>(type: "int", nullable: true),
                    AchTransactionId = table.Column<int>(type: "int", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EventStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RaisedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IncomingNachaFileIngestionId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomingNachaProcessingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_IncomingNachaFileIngestionId",
                        column: x => x.IncomingNachaFileIngestionId,
                        principalTable: "IncomingNachaFileIngestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncomingNachaProcessingEvents_IncomingNachaFileIngestions_IncomingNachaFileIngestionId1",
                        column: x => x.IncomingNachaFileIngestionId1,
                        principalTable: "IncomingNachaFileIngestions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IntegrationMappingTraceEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TraceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceField = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TargetField = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SourceValueSanitized = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MappedValueSanitized = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MappingRuleId = table.Column<long>(type: "bigint", nullable: true),
                    TransformationApplied = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DefaultValueApplied = table.Column<bool>(type: "bit", nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    UsedFallback = table.Column<bool>(type: "bit", nullable: false),
                    Missing = table.Column<bool>(type: "bit", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationMappingTraceEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationMappingTraceEntries_IntegrationMappingTraces_TraceId",
                        column: x => x.TraceId,
                        principalTable: "IntegrationMappingTraces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationMappingSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Notes = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ValidationSummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationMappingSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationMappingSets_IntegrationMethods_MethodId",
                        column: x => x.MethodId,
                        principalTable: "IntegrationMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationMethodParameters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    ParameterPath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DescriptionEs = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ExampleValue = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    UiHelpText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Cardinality = table.Column<int>(type: "int", nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationMethodParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationMethodParameters_IntegrationMethods_MethodId",
                        column: x => x.MethodId,
                        principalTable: "IntegrationMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationSourceCatalogFields",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodId = table.Column<int>(type: "int", nullable: true),
                    SourceKind = table.Column<int>(type: "int", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FieldPath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Cardinality = table.Column<int>(type: "int", nullable: false),
                    Nullable = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationSourceCatalogFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationSourceCatalogFields_IntegrationMethods_MethodId",
                        column: x => x.MethodId,
                        principalTable: "IntegrationMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "NachaRecordFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NachaRecordLayoutId = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartPosition = table.Column<int>(type: "int", nullable: false),
                    Length = table.Column<int>(type: "int", nullable: false),
                    PadChar = table.Column<string>(type: "nvarchar(1)", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(1)", nullable: false),
                    DbColumn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaRecordFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NachaRecordFields_NachaRecordLayouts_NachaRecordLayoutId",
                        column: x => x.NachaRecordLayoutId,
                        principalTable: "NachaRecordLayouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SecondLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PersonType = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_DocumentTypes_DocumentType",
                        column: x => x.DocumentType,
                        principalTable: "DocumentTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customers_GenderTypes_Gender",
                        column: x => x.Gender,
                        principalTable: "GenderTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customers_PersonTypes_PersonType",
                        column: x => x.PersonType,
                        principalTable: "PersonTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Expiration = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CfgProfile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    NameEs = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    FlowTypeId = table.Column<int>(type: "int", nullable: false),
                    DirectionId = table.Column<int>(type: "int", nullable: false),
                    ServiceClassId = table.Column<int>(type: "int", nullable: true),
                    ContextPriority = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    VersionMajor = table.Column<int>(type: "int", nullable: false),
                    VersionMinor = table.Column<int>(type: "int", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SupersedesProfileId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfgProfile", x => x.Id);
                    table.CheckConstraint("CK_CfgProfile_EffectiveRange", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_CfgProfile_VersionMajor_Positive", "\"VersionMajor\" >= 1");
                    table.CheckConstraint("CK_CfgProfile_VersionMinor_NonNegative", "\"VersionMinor\" >= 0");
                    table.ForeignKey(
                        name: "FK_CfgProfile_CatClearingHouse_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "CatClearingHouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfgProfile_CatConfigStatus_StatusId",
                        column: x => x.StatusId,
                        principalTable: "CatConfigStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfgProfile_CatDirection_DirectionId",
                        column: x => x.DirectionId,
                        principalTable: "CatDirection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfgProfile_CatFlowType_FlowTypeId",
                        column: x => x.FlowTypeId,
                        principalTable: "CatFlowType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfgProfile_CatServiceClass_ServiceClassId",
                        column: x => x.ServiceClassId,
                        principalTable: "CatServiceClass",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfgProfile_CfgProfile_SupersedesProfileId",
                        column: x => x.SupersedesProfileId,
                        principalTable: "CfgProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchReturnCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FlowType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AppliesToDebit = table.Column<bool>(type: "bit", nullable: false),
                    AppliesToCredit = table.Column<bool>(type: "bit", nullable: false),
                    AppliesToPrenotification = table.Column<bool>(type: "bit", nullable: false),
                    AppliesToReturn = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAddenda = table.Column<bool>(type: "bit", nullable: false),
                    MaxDaysAllowed = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RegulatorySource = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchReturnCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchReturnCodes_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchReturnOfReturnPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    OriginalReturnCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FlowType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AllowedNewReturnCodesCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MaxDays = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiredOriginalState = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsUniquePerTransaction = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchReturnOfReturnPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchReturnOfReturnPolicies_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchReturnPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FlowType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AllowedReturnCodesCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MaxDays = table.Column<int>(type: "int", nullable: false),
                    MaxCycles = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiredOriginalTransactionState = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AllowsReturnOfReturn = table.Column<bool>(type: "bit", nullable: false),
                    RequiresAddenda = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchReturnPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchReturnPolicies_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClearingHouseCycleConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    CycleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    CutoffTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClearingHouseCycleConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClearingHouseCycleConfigs_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClearingHouseSpecialDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClearingHouseSpecialDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClearingHouseSpecialDates_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClearingHouseTransactionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    TransactionNature = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequiresPrenotification = table.Column<bool>(type: "bit", nullable: false),
                    PrenotificationMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequiresReceiverIdentificationValidation = table.Column<bool>(type: "bit", nullable: false),
                    ReceiverIdentificationValidationMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AppliesToNachaExport = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AppliesToMonetaryTransactions = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    NormativeSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormativeReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClearingHouseTransactionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClearingHouseTransactionRules_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialInstitutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefaultSource = table.Column<bool>(type: "bit", nullable: false),
                    RoutingNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransitCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckDigit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialInstitutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialInstitutions_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CertificateLoadAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateVersionId = table.Column<int>(type: "int", nullable: false),
                    LoadSource = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ValidationResult = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ValidationErrorsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LoadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LoadedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateLoadAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateLoadAudits_DigitalCertificateVersions_CertificateVersionId",
                        column: x => x.CertificateVersionId,
                        principalTable: "DigitalCertificateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CertificateRotationHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PreviousVersionId = table.Column<int>(type: "int", nullable: false),
                    NewVersionId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RotatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RotatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TicketRef = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateRotationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateRotationHistories_DigitalCertificateVersions_NewVersionId",
                        column: x => x.NewVersionId,
                        principalTable: "DigitalCertificateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CertificateRotationHistories_DigitalCertificateVersions_PreviousVersionId",
                        column: x => x.PreviousVersionId,
                        principalTable: "DigitalCertificateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CertificateUsageLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateVersionId = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    OperationId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContextJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Result = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByProcess = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateUsageLogs_DigitalCertificateVersions_CertificateVersionId",
                        column: x => x.CertificateVersionId,
                        principalTable: "DigitalCertificateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DigitalEnvelopeOperationLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Direction = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    CertificateVersionId = table.Column<int>(type: "int", nullable: true),
                    FileNameIn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FileNameOut = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    HashPlainSha256 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    HashEncryptedSha256 = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SizeBefore = table.Column<long>(type: "bigint", nullable: true),
                    SizeAfter = table.Column<long>(type: "bigint", nullable: true),
                    Result = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalEnvelopeOperationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalEnvelopeOperationLogs_DigitalCertificateVersions_CertificateVersionId",
                        column: x => x.CertificateVersionId,
                        principalTable: "DigitalCertificateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationMappingSetHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MappingSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PerformedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationMappingSetHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationMappingSetHistory_IntegrationMappingSets_MappingSetId",
                        column: x => x.MappingSetId,
                        principalTable: "IntegrationMappingSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationMappingRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MappingSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    ParameterId = table.Column<long>(type: "bigint", nullable: false),
                    SourceKind = table.Column<int>(type: "int", nullable: false),
                    SourceCatalogFieldId = table.Column<long>(type: "bigint", nullable: true),
                    SourceFieldPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FixedValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TransformationCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    FormatMask = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    RequiredOverride = table.Column<bool>(type: "bit", nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ConditionExpression = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationMappingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationMappingRules_IntegrationMappingSets_MappingSetId",
                        column: x => x.MappingSetId,
                        principalTable: "IntegrationMappingSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationMappingRules_IntegrationMethodParameters_ParameterId",
                        column: x => x.ParameterId,
                        principalTable: "IntegrationMethodParameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationMappingRules_IntegrationSourceCatalogFields_SourceCatalogFieldId",
                        column: x => x.SourceCatalogFieldId,
                        principalTable: "IntegrationSourceCatalogFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "CustomerAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAccounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    AddressType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Line1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Line2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Colombia"),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAddresses_AddressTypes_AddressType",
                        column: x => x.AddressType,
                        principalTable: "AddressTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerAddresses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    EmailType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerEmails_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerEmails_EmailTypes_EmailType",
                        column: x => x.EmailType,
                        principalTable: "EmailTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPhones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    PhoneType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPhones_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerPhones_PhoneTypes_PhoneType",
                        column: x => x.PhoneType,
                        principalTable: "PhoneTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CfgLayoutVariant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    RecordCodeId = table.Column<int>(type: "int", nullable: false),
                    VariantCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NameEs = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    TotalLength = table.Column<int>(type: "int", nullable: false),
                    SelectionPredicateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefaultForRecord = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfgLayoutVariant", x => x.Id);
                    table.CheckConstraint("CK_CfgLayoutVariant_EffectiveRange", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.CheckConstraint("CK_CfgLayoutVariant_Priority_NonNegative", "\"Priority\" >= 0");
                    table.CheckConstraint("CK_CfgLayoutVariant_TotalLength_Positive", "\"TotalLength\" > 0");
                    table.ForeignKey(
                        name: "FK_CfgLayoutVariant_CatConfigStatus_StatusId",
                        column: x => x.StatusId,
                        principalTable: "CatConfigStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfgLayoutVariant_CatRecordCode_RecordCodeId",
                        column: x => x.RecordCodeId,
                        principalTable: "CatRecordCode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfgLayoutVariant_CfgProfile_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "CfgProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CfgProfileTag",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    TagKey = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    TagValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfgProfileTag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CfgProfileTag_CfgProfile_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "CfgProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CfgPublishRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValidationReportJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfgPublishRequest", x => x.Id);
                    table.CheckConstraint("CK_CfgPublishRequest_Status_Valid", "\"Status\" IN ('PENDING','APPROVED','REJECTED','CANCELLED')");
                    table.ForeignKey(
                        name: "FK_CfgPublishRequest_CfgProfile_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "CfgProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistConfigChange",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistConfigChange", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistConfigChange_CfgProfile_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "CfgProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistConfigSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    VersionMajor = table.Column<int>(type: "int", nullable: false),
                    VersionMinor = table.Column<int>(type: "int", nullable: false),
                    SnapshotType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistConfigSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistConfigSnapshot_CfgProfile_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "CfgProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchCycles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CycleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProcessingDate = table.Column<DateTime>(type: "date", nullable: false),
                    CutoffTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    RescheduleOnHoliday = table.Column<bool>(type: "bit", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    ClearingHouseCycleConfigId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchCycles_ClearingHouseCycleConfigs_ClearingHouseCycleConfigId",
                        column: x => x.ClearingHouseCycleConfigId,
                        principalTable: "ClearingHouseCycleConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchCycles_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstitutionClearingHousePreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialInstitutionId = table.Column<int>(type: "int", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionClearingHousePreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstitutionClearingHousePreferences_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InstitutionClearingHousePreferences_FinancialInstitutions_FinancialInstitutionId",
                        column: x => x.FinancialInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NachaFileNamingRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    SourceFinancialInstitutionId = table.Column<int>(type: "int", nullable: true),
                    FileDirection = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NamePattern = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DailySequenceMin = table.Column<int>(type: "int", nullable: false),
                    DailySequenceMax = table.Column<int>(type: "int", nullable: false),
                    InternalFileIdMappingMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequiresNameHeaderEntityMatch = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NormativeSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormativeReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaFileNamingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NachaFileNamingRules_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NachaFileNamingRules_FinancialInstitutions_SourceFinancialInstitutionId",
                        column: x => x.SourceFinancialInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NachaInboundSimulations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SimulationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    ClearingHouseName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ScenarioType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ResponseMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OriginFinancialInstitutionId = table.Column<int>(type: "int", nullable: false),
                    DestinationFinancialInstitutionId = table.Column<int>(type: "int", nullable: false),
                    EntriesCount = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CycleCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GeneratedOnly = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AutoImported = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UploadRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ExternalTransmission = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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
                        name: "FK_NachaInboundSimulations_FinancialInstitutions_DestinationFinancialInstitutionId",
                        column: x => x.DestinationFinancialInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NachaInboundSimulations_FinancialInstitutions_OriginFinancialInstitutionId",
                        column: x => x.OriginFinancialInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CfgLayoutField",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LayoutVariantId = table.Column<int>(type: "int", nullable: false),
                    FieldCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FieldNameEs = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StartPosition = table.Column<int>(type: "int", nullable: false),
                    Length = table.Column<int>(type: "int", nullable: false),
                    PadChar = table.Column<string>(type: "nvarchar(1)", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(1)", nullable: false),
                    FormatMask = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisibleInBackoffice = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SourceDefinitionId = table.Column<int>(type: "int", nullable: false),
                    TransformationPipelineJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfgLayoutField", x => x.Id);
                    table.CheckConstraint("CK_CfgLayoutField_Justification_Valid", "\"Justification\" IN ('L','R')");
                    table.CheckConstraint("CK_CfgLayoutField_Length_Positive", "\"Length\" > 0");
                    table.CheckConstraint("CK_CfgLayoutField_StartPosition_Positive", "\"StartPosition\" > 0");
                    table.ForeignKey(
                        name: "FK_CfgLayoutField_CfgFieldSourceDefinition_SourceDefinitionId",
                        column: x => x.SourceDefinitionId,
                        principalTable: "CfgFieldSourceDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfgLayoutField_CfgLayoutVariant_LayoutVariantId",
                        column: x => x.LayoutVariantId,
                        principalTable: "CfgLayoutVariant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CfgProfileRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileId = table.Column<int>(type: "int", nullable: false),
                    RecordCodeId = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MinOccurs = table.Column<int>(type: "int", nullable: false),
                    MaxOccurs = table.Column<int>(type: "int", nullable: true),
                    SourceStrategy = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LayoutVariantId = table.Column<int>(type: "int", nullable: true),
                    SemanticRuleSetId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfgProfileRecord", x => x.Id);
                    table.CheckConstraint("CK_CfgProfileRecord_MaxOccurs_Valid", "\"MaxOccurs\" IS NULL OR \"MaxOccurs\" >= \"MinOccurs\"");
                    table.CheckConstraint("CK_CfgProfileRecord_MinOccurs_NonNegative", "\"MinOccurs\" >= 0");
                    table.ForeignKey(
                        name: "FK_CfgProfileRecord_CatRecordCode_RecordCodeId",
                        column: x => x.RecordCodeId,
                        principalTable: "CatRecordCode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfgProfileRecord_CfgLayoutVariant_LayoutVariantId",
                        column: x => x.LayoutVariantId,
                        principalTable: "CfgLayoutVariant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfgProfileRecord_CfgProfile_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "CfgProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CfgProfileRecord_CfgRuleSet_SemanticRuleSetId",
                        column: x => x.SemanticRuleSetId,
                        principalTable: "CfgRuleSet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchCycleId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ServiceClassCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyIdentification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyEntryDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyEntryDescriptionId = table.Column<int>(type: "int", nullable: false),
                    OriginOrOdfi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveEntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BatchSequenceNumber = table.Column<int>(type: "int", nullable: false),
                    TotalDebitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCreditAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchBatches_AchCycles_AchCycleId",
                        column: x => x.AchCycleId,
                        principalTable: "AchCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchFileExports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchCycleId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    ExportKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    TotalRecords = table.Column<int>(type: "int", nullable: false),
                    TotalTransactions = table.Column<int>(type: "int", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchFileExports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchFileExports_AchCycles_AchCycleId",
                        column: x => x.AchCycleId,
                        principalTable: "AchCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchFileExports_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CenitCycleExecutions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchCycleId = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CenitCycleExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CenitCycleExecutions_AchCycles_AchCycleId",
                        column: x => x.AchCycleId,
                        principalTable: "AchCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    ReferenceCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: true),
                    CycleNumber = table.Column<int>(type: "int", nullable: false),
                    AchCycleId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IncomingNachaFileIngestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaHeaders", x => x.NachaID);
                    table.ForeignKey(
                        name: "FK_NachaHeaders_AchCycles_AchCycleId",
                        column: x => x.AchCycleId,
                        principalTable: "AchCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NachaHeaders_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NachaHeaders_IncomingNachaFileIngestions_IncomingNachaFileIngestionId",
                        column: x => x.IncomingNachaFileIngestionId,
                        principalTable: "IncomingNachaFileIngestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NachaInboundSimulationEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NachaInboundSimulationId = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TransactionId = table.Column<int>(type: "int", nullable: true),
                    PrenotificationReference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    AccountNumberMasked = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Nature = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ExpectedStatusAfterUpload = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsSynthetic = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaInboundSimulationEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NachaInboundSimulationEntries_NachaInboundSimulations_NachaInboundSimulationId",
                        column: x => x.NachaInboundSimulationId,
                        principalTable: "NachaInboundSimulations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CfgFieldRule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LayoutFieldId = table.Column<int>(type: "int", nullable: false),
                    RuleTypeId = table.Column<int>(type: "int", nullable: false),
                    RuleCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ErrorMessageEs = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ConditionDsl = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RuleConfigJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfgFieldRule", x => x.Id);
                    table.CheckConstraint("CK_CfgFieldRule_Severity_Valid", "\"Severity\" IN ('ERROR','WARN')");
                    table.ForeignKey(
                        name: "FK_CfgFieldRule_CatRuleType_RuleTypeId",
                        column: x => x.RuleTypeId,
                        principalTable: "CatRuleType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CfgFieldRule_CfgLayoutField_LayoutFieldId",
                        column: x => x.LayoutFieldId,
                        principalTable: "CfgLayoutField",
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
                    TransactionExternalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServiceClassCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyEntryDescriptionId = table.Column<int>(type: "int", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyIdentification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginatingDFI = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivingDFI = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TraceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TraceSequenceNumber = table.Column<int>(type: "int", nullable: false),
                    EffectiveEntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddendaRecordIndicator = table.Column<bool>(type: "bit", nullable: false),
                    IsPrenotification = table.Column<bool>(type: "bit", nullable: false),
                    State = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Pending"),
                    StateChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SlaDeadlineAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContrapartidasResponseCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ReturnReasonCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OriginalTraceRef = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecipientIdNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiscretionaryData = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    SourceAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DestinationAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceInstitutionId = table.Column<int>(type: "int", nullable: false),
                    DestinationInstitutionId = table.Column<int>(type: "int", nullable: false),
                    AchCycleId = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    AchBatchId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchTransactions_AchBatches_AchBatchId",
                        column: x => x.AchBatchId,
                        principalTable: "AchBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchTransactions_AchCycles_AchCycleId",
                        column: x => x.AchCycleId,
                        principalTable: "AchCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchTransactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id");
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

            migrationBuilder.CreateTable(
                name: "ContrapartidaDispatchBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchCycleId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    AchBatchId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TriggeredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalItems = table.Column<int>(type: "int", nullable: false),
                    TotalSucceeded = table.Column<int>(type: "int", nullable: false),
                    TotalFailed = table.Column<int>(type: "int", nullable: false),
                    TotalPartial = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    JobId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MappingSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MappingVersion = table.Column<int>(type: "int", nullable: true),
                    MappingSnapshotHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequestPayloadXml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsePayloadXml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SummaryMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContrapartidaDispatchBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContrapartidaDispatchBatches_AchBatches_AchBatchId",
                        column: x => x.AchBatchId,
                        principalTable: "AchBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContrapartidaDispatchBatches_AchCycles_AchCycleId",
                        column: x => x.AchCycleId,
                        principalTable: "AchCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContrapartidaDispatchBatches_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CenitNettingExecutions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CenitCycleExecutionId = table.Column<long>(type: "bigint", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalDebit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCredit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CenitNettingExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CenitNettingExecutions_CenitCycleExecutions_CenitCycleExecutionId",
                        column: x => x.CenitCycleExecutionId,
                        principalTable: "CenitCycleExecutions",
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
                    BusinessType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IdUserOrig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurposeOfTransaction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceOrAccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InfofromOriginator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CollectorId = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    ReceiverCustomerCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ServiceDescription = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    ReturnReasonCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    OriginalTraceNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    NewTraceNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
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
                    EntryHash = table.Column<long>(type: "bigint", nullable: true),
                    TotalDebitAmount = table.Column<decimal>(type: "money", nullable: false),
                    TotalCreditAmount = table.Column<decimal>(type: "money", nullable: false),
                    IdUserOrig = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CodAutMessage = table.Column<string>(type: "nvarchar(19)", maxLength: 19, nullable: true),
                    Reserved = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    IdOrigEntity = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    BatchNumber = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
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
                    EntryHash = table.Column<long>(type: "bigint", nullable: false),
                    TotalDebitAmount = table.Column<decimal>(type: "money", nullable: false),
                    TotalCreditAmount = table.Column<decimal>(type: "money", nullable: false),
                    Reserved = table.Column<string>(type: "nvarchar(39)", maxLength: 39, nullable: true),
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
                name: "AchReturnsGenerated",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalTransactionId = table.Column<int>(type: "int", nullable: false),
                    ReturnCycleId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReturnReasonCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewSequenceNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    OriginalSequenceNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ReceiverEntityCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    OriginatorEntityCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchReturnsGenerated", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchReturnsGenerated_AchCycles_ReturnCycleId",
                        column: x => x.ReturnCycleId,
                        principalTable: "AchCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AchReturnsGenerated_AchTransactions_OriginalTransactionId",
                        column: x => x.OriginalTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AchTransactionAddenda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchTransactionId = table.Column<int>(type: "int", nullable: false),
                    AddendaType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BusinessType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Information = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(53)", maxLength: 53, nullable: true),
                    CollectorId = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: true),
                    ReceiverCustomerCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ServiceDescription = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    ReturnReasonCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    OriginalTraceNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    NewTraceNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchTransactionAddenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchTransactionAddenda_AchTransactions_AchTransactionId",
                        column: x => x.AchTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AchTransactionStateEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchTransactionId = table.Column<int>(type: "int", nullable: false),
                    FromState = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ToState = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchTransactionStateEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchTransactionStateEvents_AchTransactions_AchTransactionId",
                        column: x => x.AchTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CenitCycleQueues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchTransactionId = table.Column<int>(type: "int", nullable: false),
                    TargetAchCycleId = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    OriginalAchCycleId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QueueReason = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EnqueuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DequeuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CenitCycleExecutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CenitCycleQueues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CenitCycleQueues_AchCycles_TargetAchCycleId",
                        column: x => x.TargetAchCycleId,
                        principalTable: "AchCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CenitCycleQueues_AchTransactions_AchTransactionId",
                        column: x => x.AchTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CenitCycleQueues_CenitCycleExecutions_CenitCycleExecutionId",
                        column: x => x.CenitCycleExecutionId,
                        principalTable: "CenitCycleExecutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContrapartidaDispatchItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchTransactionId = table.Column<int>(type: "int", nullable: false),
                    AchCycleId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    AchBatchId = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSuccessAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastResponseCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastErrorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LastCorrelationId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LastDispatchedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContrapartidaDispatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContrapartidaDispatchItems_AchBatches_AchBatchId",
                        column: x => x.AchBatchId,
                        principalTable: "AchBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContrapartidaDispatchItems_AchCycles_AchCycleId",
                        column: x => x.AchCycleId,
                        principalTable: "AchCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContrapartidaDispatchItems_AchTransactions_AchTransactionId",
                        column: x => x.AchTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContrapartidaDispatchItems_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerThirdParties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    DestinationInstitutionId = table.Column<int>(type: "int", nullable: false),
                    DestinationAccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecipientIdNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PrenotificationTransactionId = table.Column<int>(type: "int", nullable: true),
                    ValidationCycleId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidationMessage = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerThirdParties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerThirdParties_AchTransactions_PrenotificationTransactionId",
                        column: x => x.PrenotificationTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerThirdParties_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerThirdParties_FinancialInstitutions_DestinationInstitutionId",
                        column: x => x.DestinationInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LiquidityOptimizationDecisions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CenitCycleExecutionId = table.Column<long>(type: "bigint", nullable: false),
                    AchTransactionId = table.Column<int>(type: "int", nullable: false),
                    AchBatchId = table.Column<int>(type: "int", nullable: false),
                    ValueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    ClearingHouseCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SourceFileReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DecisionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    DecisionReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LiquidityModelUsed = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FromCycleId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ToCycleId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiquidityOptimizationDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiquidityOptimizationDecisions_AchTransactions_AchTransactionId",
                        column: x => x.AchTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LiquidityOptimizationDecisions_CenitCycleExecutions_CenitCycleExecutionId",
                        column: x => x.CenitCycleExecutionId,
                        principalTable: "CenitCycleExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReturnOfReturnFlows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceReturnTransactionId = table.Column<int>(type: "int", nullable: false),
                    ReturnOfReturnTransactionId = table.Column<int>(type: "int", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrchestratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CenitCycleExecutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnOfReturnFlows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnOfReturnFlows_AchTransactions_ReturnOfReturnTransactionId",
                        column: x => x.ReturnOfReturnTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnOfReturnFlows_AchTransactions_SourceReturnTransactionId",
                        column: x => x.SourceReturnTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnOfReturnFlows_CenitCycleExecutions_CenitCycleExecutionId",
                        column: x => x.CenitCycleExecutionId,
                        principalTable: "CenitCycleExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CenitNetPositions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CenitNettingExecutionId = table.Column<long>(type: "bigint", nullable: false),
                    FinancialInstitutionId = table.Column<int>(type: "int", nullable: false),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExternalLiquidity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SimulatedLiquidity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LiquiditySourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AvailableLiquidity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HasInsufficientFunds = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CenitNetPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CenitNetPositions_CenitNettingExecutions_CenitNettingExecutionId",
                        column: x => x.CenitNettingExecutionId,
                        principalTable: "CenitNettingExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CenitNetPositions_FinancialInstitutions_FinancialInstitutionId",
                        column: x => x.FinancialInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CenitNettingDetails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CenitNettingExecutionId = table.Column<long>(type: "bigint", nullable: false),
                    AchTransactionId = table.Column<int>(type: "int", nullable: false),
                    SourceInstitutionId = table.Column<int>(type: "int", nullable: false),
                    DestinationInstitutionId = table.Column<int>(type: "int", nullable: false),
                    AchBatchId = table.Column<int>(type: "int", nullable: false),
                    ValueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    ClearingHouseCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SourceFileReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IncludedInSettlement = table.Column<bool>(type: "bit", nullable: false),
                    DecisionReason = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CenitNettingDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CenitNettingDetails_AchTransactions_AchTransactionId",
                        column: x => x.AchTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CenitNettingDetails_CenitNettingExecutions_CenitNettingExecutionId",
                        column: x => x.CenitNettingExecutionId,
                        principalTable: "CenitNettingExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncomingNachaEntryClassifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncomingNachaFileIngestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryDetailId = table.Column<int>(type: "int", nullable: false),
                    AddendaRecordId = table.Column<int>(type: "int", nullable: true),
                    FunctionalClass = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EligibilityStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequiresLink = table.Column<bool>(type: "bit", nullable: false),
                    RequiresManualResolution = table.Column<bool>(type: "bit", nullable: false),
                    OriginalTraceRef = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReturnReasonCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PrenoteStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BusinessMeaning = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClassifierVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ClassificationEvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomingNachaEntryClassifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncomingNachaEntryClassifications_AddendaRecords_AddendaRecordId",
                        column: x => x.AddendaRecordId,
                        principalTable: "AddendaRecords",
                        principalColumn: "AddendaID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IncomingNachaEntryClassifications_EntryDetails_EntryDetailId",
                        column: x => x.EntryDetailId,
                        principalTable: "EntryDetails",
                        principalColumn: "EntryDetailID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncomingNachaEntryClassifications_IncomingNachaFileIngestions_IncomingNachaFileIngestionId",
                        column: x => x.IncomingNachaFileIngestionId,
                        principalTable: "IncomingNachaFileIngestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncomingNachaTransactionLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncomingNachaFileIngestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryDetailId = table.Column<int>(type: "int", nullable: true),
                    AddendaRecordId = table.Column<int>(type: "int", nullable: true),
                    AchTransactionId = table.Column<int>(type: "int", nullable: true),
                    LinkType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LinkedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomingNachaTransactionLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncomingNachaTransactionLinks_AchTransactions_AchTransactionId",
                        column: x => x.AchTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IncomingNachaTransactionLinks_AddendaRecords_AddendaRecordId",
                        column: x => x.AddendaRecordId,
                        principalTable: "AddendaRecords",
                        principalColumn: "AddendaID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IncomingNachaTransactionLinks_EntryDetails_EntryDetailId",
                        column: x => x.EntryDetailId,
                        principalTable: "EntryDetails",
                        principalColumn: "EntryDetailID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_IncomingNachaTransactionLinks_IncomingNachaFileIngestions_IncomingNachaFileIngestionId",
                        column: x => x.IncomingNachaFileIngestionId,
                        principalTable: "IncomingNachaFileIngestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContrapartidaDispatchAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispatchItemId = table.Column<long>(type: "bigint", nullable: false),
                    DispatchBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RetryEligible = table.Column<bool>(type: "bit", nullable: false),
                    ExternalResponseCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExternalResponseMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequestPayloadXml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsePayloadXml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContrapartidaDispatchAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContrapartidaDispatchAttempts_ContrapartidaDispatchBatches_DispatchBatchId",
                        column: x => x.DispatchBatchId,
                        principalTable: "ContrapartidaDispatchBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContrapartidaDispatchAttempts_ContrapartidaDispatchItems_DispatchItemId",
                        column: x => x.DispatchItemId,
                        principalTable: "ContrapartidaDispatchItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AchReturnOfReturnGeneratedFileAuditFlows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AchReturnOfReturnGeneratedFileAuditId = table.Column<int>(type: "int", nullable: false),
                    ReturnOfReturnFlowId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchReturnOfReturnGeneratedFileAuditFlows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchReturnOfReturnGeneratedFileAuditFlows_AchReturnOfReturnGeneratedFileAudits_AchReturnOfReturnGeneratedFileAuditId",
                        column: x => x.AchReturnOfReturnGeneratedFileAuditId,
                        principalTable: "AchReturnOfReturnGeneratedFileAudits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AchReturnOfReturnGeneratedFileAuditFlows_ReturnOfReturnFlows_ReturnOfReturnFlowId",
                        column: x => x.ReturnOfReturnFlowId,
                        principalTable: "ReturnOfReturnFlows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncomingNachaDispatchQueue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncomingNachaFileIngestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncomingNachaEntryClassificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncomingNachaTransactionLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchTransactionId = table.Column<int>(type: "int", nullable: false),
                    AchCycleId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "int", nullable: false),
                    OperationalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QueueStatus = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IdempotencyDispatchKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastErrorCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    LastResponseCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomingNachaDispatchQueue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncomingNachaDispatchQueue_AchTransactions_AchTransactionId",
                        column: x => x.AchTransactionId,
                        principalTable: "AchTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncomingNachaDispatchQueue_IncomingNachaEntryClassifications_IncomingNachaEntryClassificationId",
                        column: x => x.IncomingNachaEntryClassificationId,
                        principalTable: "IncomingNachaEntryClassifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncomingNachaDispatchQueue_IncomingNachaFileIngestions_IncomingNachaFileIngestionId",
                        column: x => x.IncomingNachaFileIngestionId,
                        principalTable: "IncomingNachaFileIngestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncomingNachaDispatchQueue_IncomingNachaTransactionLinks_IncomingNachaTransactionLinkId",
                        column: x => x.IncomingNachaTransactionLinkId,
                        principalTable: "IncomingNachaTransactionLinks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncomingNachaIntegrationExecution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispatchQueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MappingSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MappingVersion = table.Column<int>(type: "int", nullable: true),
                    MappingSnapshotHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ResponseHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestPayloadXml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsePayloadXml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ResponseMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    IsRetryable = table.Column<bool>(type: "bit", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomingNachaIntegrationExecution", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncomingNachaIntegrationExecution_IncomingNachaDispatchQueue_DispatchQueueId",
                        column: x => x.DispatchQueueId,
                        principalTable: "IncomingNachaDispatchQueue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AddressTypes",
                columns: new[] { "Code", "Description", "Name" },
                values: new object[,]
                {
                    { "CASA", null, "Casa" },
                    { "CORRESPONDENCIA", null, "Correspondencia" },
                    { "FINCA", null, "Finca" },
                    { "PERSONAL", null, "Personal" },
                    { "TRABAJO", null, "Trabajo" }
                });

            migrationBuilder.InsertData(
                table: "CatClearingHouse",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "ACH", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "ACH Colombia", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, "CENIT", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "CENIT", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "CatConfigStatus",
                columns: new[] { "Id", "Code", "CreatedAt", "IsEditable", "IsPublishable", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "BORRADOR", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, true, new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, "PUBLICADO", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, false, new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, "INACTIVO", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, false, new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, "ARCHIVADO", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, false, new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "CatDataSourceType",
                columns: new[] { "Id", "Code", "CreatedAt", "NameEs", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "CONSTANTE", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Valor constante", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, "ENTIDAD", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Entidad del dominio", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, "SQL_VIEW", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Vista SQL", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, "SQL_PROCEDURE", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Procedimiento SQL", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, "EXPRESION", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Expresión declarativa", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "CatDirection",
                columns: new[] { "Id", "Code", "CreatedAt", "IsActive", "NameEs", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "ENTRADA", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Entrada", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, "SALIDA", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Salida", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "CatFlowType",
                columns: new[] { "Id", "Code", "CreatedAt", "DirectionDefaultId", "IsActive", "NameEs", "UpdatedAt" },
                values: new object[] { 7, "OTRO", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Otro", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "CatRecordCode",
                columns: new[] { "Id", "Code", "CreatedAt", "IsMandatoryBase", "NameEs", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "1", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Encabezado de archivo", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, "5", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Encabezado de lote", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, "6", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Detalle de transacción", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, "7", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, "Addenda", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, "8", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Control de lote", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6, "9", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Control de archivo", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "CatRuleType",
                columns: new[] { "Id", "Code", "CreatedAt", "NameEs", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "REQUIRED", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Obligatorio", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, "REGEX", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Expresión regular", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, "RANGE", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Rango", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, "ENUM", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Lista de valores", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, "DATE_FORMAT", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Formato de fecha", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6, "CHECKSUM", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Dígito de chequeo", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 7, "CONDITIONAL", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Condicional", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 8, "CROSS_FIELD", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Validación entre campos", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "CatServiceClass",
                columns: new[] { "Id", "ClearingHouseId", "Code", "CreatedAt", "IsActive", "NameEs", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, "PPD", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Pago prearreglado y depósito", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, null, "CCD", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Concentración y desembolso corporativo", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "CompanyEntryDescription",
                columns: new[] { "Id", "Description", "IsActive", "StandardEntryClassCode", "Term" },
                values: new object[,]
                {
                    { 1, "Pagos de administración.", true, "PPD", "ADMONES" },
                    { 2, "Fondos destinados a ahorro.", true, "PPD", "AHORROS" },
                    { 3, "Pagos de aportes.", true, "PPD", "APORTES" },
                    { 4, "Pagos de arrendamientos o arriendos.", true, "PPD", "ARRIENDOS" },
                    { 5, "Pagos de servicios de buscapersonas.", true, "PPD", "BEEPERS" },
                    { 6, "Pagos de cédulas de capitalización (Ajustado al límite de 10 caracteres).", true, "PPD", "CEDULAS CP" },
                    { 7, "Pagos de servicios de telefonía móvil.", true, "PPD", "CELULARES" },
                    { 8, "Pagos de fondos de cesantías.", true, "PPD", "CESANTIAS" },
                    { 9, "Pagos de cuotas de clubes.", true, "PPD", "CLUBS" },
                    { 10, "Pagos de matrículas o pensiones escolares.", true, "PPD", "COLEGIOS" },
                    { 11, "Pagos de comisiones.", true, "PPD", "COMISIONES" },
                    { 12, "Pagos a contratistas.", true, "PPD", "CONTRATISTS" },
                    { 13, "Pagos de dividendos o acciones.", true, "PPD", "DIVIDENDOS" },
                    { 14, "Pagos por concepto de donaciones.", true, "PPD", "DONACIONES" },
                    { 15, "Pagos de servicios profesionales u honorarios.", true, "PPD", "HONORARIOS" },
                    { 16, "Pagos de obligaciones tributarias.", true, "PPD", "IMPUESTOS" },
                    { 17, "Pagos de rendimientos financieros o intereses.", true, "PPD", "INTERESES" },
                    { 18, "Pagos de nóminas de empleados.", true, "PPD", "NOMINAS" },
                    { 19, "Otros tipos de pagos no categorizados.", true, "PPD", "OTROS" },
                    { 20, "Pagos de mesadas pensionales.", true, "PPD", "PENSIONES" },
                    { 21, "Pagos de servicios de medicina prepagada.", true, "PPD", "PREPAGADAS" },
                    { 22, "Pagos de cuotas de créditos o préstamos.", true, "PPD", "PRESTAMOS" },
                    { 23, "Pagos a proveedores (Ajustado al límite de 10 caracteres).", true, "PPD", "PROVEEDORS" },
                    { 24, "Pagos de rendimientos financieros.", true, "PPD", "RENDIMIENTS" },
                    { 25, "Pagos de Riesgos Profesionales.", true, "PPD", "RIESGOS P" },
                    { 26, "Pagos de primas de seguros.", true, "PPD", "SEGUROS" },
                    { 27, "Pagos de servicios públicos (Ajustado al límite de 10 caracteres).", true, "PPD", "SERVS PUBL" },
                    { 28, "Pagos de suscripciones a revistas o servicios.", true, "PPD", "SUSCRIPCIS" },
                    { 29, "Pagos de cuotas de tarjetas de crédito.", true, "PPD", "TARCREDITS" },
                    { 30, "Transferencias de fondos entre cuentas (Obligatorio para personas naturales).", true, "PPD", "TRASLADOS" },
                    { 31, "Pagos de servicios de televisión por cable.", true, "PPD", "TVS X CABL" },
                    { 32, "Pagos de servicios de televisión satelital.", true, "PPD", "TVS SATEL" },
                    { 33, "Pagos de matrículas o pensiones universitarias.", true, "PPD", "UNIVERSIDAS" },
                    { 34, "Nuevo concepto para transacciones crédito generadas por PSE.", true, "CCD", "MULTICREDITS" },
                    { 35, "Transacciones de comercio electrónico a través del botón de pagos.", true, "CCD", "PAGOS PSE" },
                    { 36, "Recaudos originados por el sistema PSE.", true, "CCD", "COBROS PSE" },
                    { 37, "Pagos de impuestos a la Dirección de Impuestos y Aduanas Nacionales.", true, "CCD", "PAGOS DIAN" },
                    { 38, "Pagos y recaudos del Sistema de Seguridad Social", true, "CCD", "COBROS SSS" }
                });

            migrationBuilder.InsertData(
                table: "DocumentTypes",
                columns: new[] { "Code", "Description", "Name" },
                values: new object[,]
                {
                    { "CC", null, "Cédula de Ciudadanía" },
                    { "CE", null, "Cédula de Extranjería" },
                    { "NIT", null, "Número de Identificación Tributaria" },
                    { "OTRO", null, "Otro" },
                    { "PAS", null, "Pasaporte" },
                    { "TI", null, "Tarjeta de Identidad" }
                });

            migrationBuilder.InsertData(
                table: "EmailTypes",
                columns: new[] { "Code", "Description", "Name" },
                values: new object[,]
                {
                    { "OTRO", null, "Otro" },
                    { "PERSONAL", null, "Personal" },
                    { "TRABAJO", null, "Trabajo" }
                });

            migrationBuilder.InsertData(
                table: "GenderTypes",
                columns: new[] { "Code", "Description", "Name" },
                values: new object[,]
                {
                    { "FEMENINO", null, "Femenino" },
                    { "MASCULINO", null, "Masculino" },
                    { "NO_BINARIO", null, "No binario" },
                    { "NO_ESPECIFICA", null, "No especifica" },
                    { "OTRO", null, "Otro" }
                });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[] { 1, "Menú principal", true, "Principal" });

            migrationBuilder.InsertData(
                table: "NachaFileIdentifierMap",
                columns: new[] { "Id", "Identifier", "Sequence" },
                values: new object[,]
                {
                    { 1, "A", 1 },
                    { 2, "B", 2 },
                    { 3, "C", 3 },
                    { 4, "D", 4 },
                    { 5, "E", 5 },
                    { 6, "F", 6 },
                    { 7, "G", 7 },
                    { 8, "H", 8 },
                    { 9, "I", 9 },
                    { 10, "J", 10 },
                    { 11, "K", 11 },
                    { 12, "L", 12 },
                    { 13, "M", 13 },
                    { 14, "N", 14 },
                    { 15, "O", 15 },
                    { 16, "P", 16 },
                    { 17, "Q", 17 },
                    { 18, "R", 18 },
                    { 19, "S", 19 },
                    { 20, "T", 20 },
                    { 21, "U", 21 },
                    { 22, "V", 22 },
                    { 23, "W", 23 },
                    { 24, "X", 24 },
                    { 25, "Y", 25 },
                    { 26, "Z", 26 },
                    { 27, "0", 27 },
                    { 28, "1", 28 },
                    { 29, "2", 29 },
                    { 30, "3", 30 },
                    { 31, "4", 31 },
                    { 32, "5", 32 },
                    { 33, "6", 33 },
                    { 34, "7", 34 },
                    { 35, "8", 35 },
                    { 36, "9", 36 }
                });

            migrationBuilder.InsertData(
                table: "NachaRecordDefinitions",
                columns: new[] { "Id", "CreatedAt", "FilterKey", "IsEnabled", "RecordCode", "Sequence", "SourceName", "SourceType", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "1", 10, null, 0, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "5", 20, null, 0, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "BatchId", true, "6", 30, "AchTransaction", 0, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "BatchId", true, "7", 40, "AchTransactionAddenda", 0, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "8", 50, null, 0, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "9", 60, null, 0, new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), "Consulta de alias", "CanReadAliases" },
                    { new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7"), "Consulta de operaciones ACH", "CanReadAch" },
                    { new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a"), "Gestión completa de operaciones ACH", "CanManageAch" },
                    { new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf"), "Gestión de usuarios", "CanManageUsers" },
                    { new Guid("dd0e54be-b6df-4ab3-8783-0f72b6e774a2"), "Consulta de catálogos", "CanReadCatalogs" }
                });

            migrationBuilder.InsertData(
                table: "PersonTypes",
                columns: new[] { "Code", "Description", "Name" },
                values: new object[,]
                {
                    { "PJ", null, "Persona jurídica" },
                    { "PN", null, "Persona natural" }
                });

            migrationBuilder.InsertData(
                table: "PhoneTypes",
                columns: new[] { "Code", "Description", "Name" },
                values: new object[,]
                {
                    { "FIJO", null, "Fijo" },
                    { "MOVIL", null, "Móvil" },
                    { "TRABAJO", null, "Trabajo" }
                });

            migrationBuilder.InsertData(
                table: "ReturnReasons",
                columns: new[] { "Id", "Category", "Code", "Description", "IsForReturn" },
                values: new object[,]
                {
                    { 1, "R", "R01", "Fondos Insuficientes.", true },
                    { 2, "R", "R02", "Cuenta o Depósito Electrónico Cerrado (Saldado o Cancelado).", true },
                    { 3, "R", "R03", "Cuenta o Depósito Electrónico No Abierto.", true },
                    { 4, "R", "R04", "Número Cuenta o Depósito Electrónico Inválido.", true },
                    { 5, "R", "R06", "Devolución Solicitada por el participante originador (por error o listas restrictivas).", true },
                    { 6, "R", "R07", "Autorización de Recaudo Revocada por el usuario Receptor.", true },
                    { 7, "R", "R08", "Orden de No Pago (instrucción específica del usuario).", true },
                    { 8, "R", "R09", "Fondos no Disponibles (saldo total suficiente, pero no el disponible).", true },
                    { 9, "R", "R10", "No existe prenotificación (para débitos).", true },
                    { 10, "R", "R12", "Usuario Originador no autorizado o Sucursal Vendida.", true },
                    { 11, "R", "R13", "Devolución por solicitud del usuario Receptor (Persona Natural: fecha/monto errado o duplicidad).", true },
                    { 12, "R", "R14", "Muerte del delegado o Representante.", true },
                    { 13, "R", "R15", "Muerte del Beneficiario o Titular de la Cuenta.", true },
                    { 14, "R", "R16", "Cuenta o Depósito Electrónico Inactivo / Bloqueado.", true },
                    { 15, "R", "R17", "La Identificación no coincide con Cuenta.", true },
                    { 16, "R", "R20", "Cuenta No Habilitada (listas restrictivas OFAC/ONU o fines políticos).", true },
                    { 17, "R", "R23", "Devolución crédito por solicitud del usuario (sobrepago, litigio, etc.).", true },
                    { 18, "R", "R29", "Devolución débito por solicitud del usuario (Persona Jurídica).", true }
                });

            migrationBuilder.InsertData(
                table: "ReturnReasons",
                columns: new[] { "Id", "Category", "Code", "Description" },
                values: new object[,]
                {
                    { 19, "R", "R30", "Cliente Receptor no habilitado para depósitos electrónicos." },
                    { 20, "R", "R31", "Prenotificación no procesada por falta de información en adenda." }
                });

            migrationBuilder.InsertData(
                table: "ReturnReasons",
                columns: new[] { "Id", "Category", "Code", "Description", "IsForReturn" },
                values: new object[,]
                {
                    { 21, "R", "R32", "Transacción crédito no procesada por falta de información en adenda.", true },
                    { 22, "R", "R33", "Excede los límites establecidos para Depósitos Electrónicos.", true },
                    { 23, "R", "R35", "Tipo de Cuenta Errada.", true }
                });

            migrationBuilder.InsertData(
                table: "ReturnReasons",
                columns: new[] { "Id", "Category", "Code", "Description" },
                values: new object[,]
                {
                    { 24, "D", "D01", "Fecha efectiva menor a la fecha de proceso." },
                    { 25, "D", "D02", "Fecha efectiva no es válida." },
                    { 26, "D", "D03", "Valor de la transacción no es numérico." },
                    { 27, "D", "D04", "Valor de prenotificación es diferente de cero." },
                    { 28, "D", "D05", "Valor de transacción monetaria es igual a cero." },
                    { 29, "D", "D06", "Excede el monto diario permitido por usuario/cuenta." },
                    { 30, "D", "D07", "Transacción excede el límite y no fue autorizada." },
                    { 31, "D", "D08", "Número de secuencia del registro adenda incorrecto." },
                    { 32, "D", "D09", "Indicador de adenda no concuerda con el registro." },
                    { 33, "D", "D10", "Causal de devolución en adenda es incorrecta." },
                    { 34, "D", "D11", "Número mínimo de registros adenda diferente de 1." },
                    { 35, "D", "D12", "Código de tipo de registro adenda es incorrecto (debe ser 05 o 99)." },
                    { 36, "D", "D13", "Datos obligatorios en débito (referencia/servicio) vacíos o en cero." },
                    { 37, "D", "D14", "Código de cliente originador por servicio no numérico." },
                    { 38, "D", "D15", "Nombre del usuario originador vacío." },
                    { 39, "D", "D16", "ID del originador en control de lote no coincide con encabezado." },
                    { 40, "D", "D17", "Número de cuenta receptora no válida." },
                    { 41, "D", "D18", "Número de lote en control no coincide con encabezado." },
                    { 42, "D", "D19", "Código de participante originador vacío o en ceros." },
                    { 43, "D", "D20", "Código clase de transacción en control de lote inválido." },
                    { 44, "D", "D21", "Código de participante receptor no válido o no está en producción." },
                    { 45, "D", "D22", "Nombre del cliente receptor vacío." },
                    { 46, "D", "D23", "ID del usuario receptor vacío." },
                    { 47, "D", "D24", "Descripción del lote vacía." },
                    { 48, "D", "D25", "Descripción de lote de Cuentas de Préstamo no válida." },
                    { 49, "D", "D26", "Campo alfanumérico no alineado a la izquierda." },
                    { 50, "D", "D27", "Caracteres especiales inválidos en la línea." },
                    { 51, "D", "D28", "Devolución de una devolución." },
                    { 52, "D", "D29", "Devolución débito tardía." },
                    { 53, "D", "D30", "Entidad Participante no puede procesar débitos." },
                    { 54, "D", "D31", "Lote duplicado en el mismo día sin autorización." },
                    { 55, "D", "D32", "Transacción no permitida en este ciclo." }
                });

            migrationBuilder.InsertData(
                table: "ReturnReasons",
                columns: new[] { "Id", "Category", "Code", "Description", "IsForReturn" },
                values: new object[] { 56, "R", "DEV14", "No consentimiento / revocación expresa del usuario receptor.", true });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1"), "Acceso completo al sistema", "Admin" },
                    { new Guid("a51746c2-0710-4d79-97b1-5b4368326f56"), "Operaciones básicas sobre ACH", "ACH.Operator" }
                });

            migrationBuilder.InsertData(
                table: "TransactionCodes",
                columns: new[] { "Code", "Description", "Name" },
                values: new object[,]
                {
                    { "21", "Devolución de crédito a cuenta corriente.", "Devolución crédito cuenta corriente" },
                    { "22", "Crédito a cuenta corriente.", "Crédito cuenta corriente" },
                    { "23", "Prenotificación crédito a cuenta corriente.", "Prenotificación crédito cuenta corriente" },
                    { "26", "Devolución de débito a cuenta corriente.", "Devolución débito cuenta corriente" },
                    { "27", "Débito a cuenta corriente.", "Débito cuenta corriente" },
                    { "28", "Prenotificación débito a cuenta corriente.", "Prenotificación débito cuenta corriente" },
                    { "31", "Devolución de crédito a cuenta de ahorros.", "Devolución crédito cuenta ahorros" },
                    { "32", "Crédito a cuenta de ahorros.", "Crédito cuenta ahorros" },
                    { "33", "Prenotificación crédito a cuenta de ahorros.", "Prenotificación crédito cuenta ahorros" },
                    { "36", "Devolución de débito a cuenta de ahorros.", "Devolución débito cuenta ahorros" },
                    { "37", "Débito a cuenta de ahorros.", "Débito cuenta ahorros" },
                    { "38", "Prenotificación débito a cuenta de ahorros.", "Prenotificación débito cuenta ahorros" },
                    { "42", "Código especial para crédito en cuenta puente (p. ej. recaudos PSE).", "Crédito cuenta puente" },
                    { "51", "Devolución de crédito a depósitos electrónicos.", "Devolución crédito depósitos electrónicos" },
                    { "52", "Crédito a depósitos electrónicos.", "Crédito depósitos electrónicos" },
                    { "53", "Prenotificación crédito a depósitos electrónicos.", "Prenotificación crédito depósitos electrónicos" },
                    { "55", "Débito a depósitos electrónicos.", "Débito depósitos electrónicos" },
                    { "56", "Devolución de débito a depósitos electrónicos.", "Devolución débito depósitos electrónicos" },
                    { "57", "Prenotificación débito a depósitos electrónicos.", "Prenotificación débito depósitos electrónicos" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "LockoutEnd", "PasswordHash", "UpdatedAt", "Username" },
                values: new object[] { new Guid("0f7d6a26-df0e-4b14-8734-3280c1da6e3d"), new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "admin@achinterbank.local", "Administrador ACH", true, null, "3eb3fe66b31e3b4d10fa70b5cad49c7112294af6ae4e476a1c405155d45aa121", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "admin" });

            migrationBuilder.InsertData(
                table: "CatFlowType",
                columns: new[] { "Id", "Code", "CreatedAt", "DirectionDefaultId", "IsActive", "NameEs", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "ORIGINAL", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, true, "Original", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2, "PRENOTIFICACION", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, true, "Prenotificación", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3, "DEVOLUCION", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, true, "Devolución", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4, "RETORNO", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, true, "Retorno", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5, "REVERSO", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, true, "Reverso", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6, "RECHAZO", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1, true, "Rechazo", new DateTimeOffset(new DateTime(2026, 4, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Exact", "Icon", "IsActive", "Label", "MenuId", "Order", "ParentId", "Route" },
                values: new object[,]
                {
                    { 1, true, "dashboard", true, "Panel principal", 1, 1, null, "/dashboard" },
                    { 2, false, "group", true, "Usuarios", 1, 2, null, "/users" },
                    { 4, true, "schedule", true, "Ciclos ACH", 1, 4, null, "/ach-cycles" },
                    { 5, false, "inventory", true, "Catálogos", 1, 5, null, "/catalogs" },
                    { 6, false, "swap_horiz", true, "Transacciones", 1, 6, null, "/transactions" },
                    { 9, true, "category", true, "Navegación", 1, 7, null, "/navigation/menu-items" },
                    { 10, true, "security", true, "Seguridad NACHA", 1, 8, null, "/nacha-security/certificates" },
                    { 18, false, "timer", true, "Programador", 1, 9, null, "/scheduler" },
                    { 20, true, "tune", true, "Configuración NACHA-M", 1, 2, null, "/nacha-config-admin/perfiles" },
                    { 27, false, "summarize", true, "Logs", 1, 10, null, "/logs" },
                    { 29, false, "hub", true, "Integraciones", 1, 5, null, "/integraciones" },
                    { 33, false, "science", true, "UAT / Simuladores", 1, 11, null, "/uat" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("dd0e54be-b6df-4ab3-8783-0f72b6e774a2"), new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { new Guid("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { new Guid("dd0e54be-b6df-4ab3-8783-0f72b6e774a2"), new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1"), new Guid("0f7d6a26-df0e-4b14-8734-3280c1da6e3d"), new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("a51746c2-0710-4d79-97b1-5b4368326f56"), new Guid("0f7d6a26-df0e-4b14-8734-3280c1da6e3d"), new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "MenuItemPermissions",
                columns: new[] { "MenuItemId", "PermissionId" },
                values: new object[,]
                {
                    { 2, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") },
                    { 4, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 5, new Guid("dd0e54be-b6df-4ab3-8783-0f72b6e774a2") },
                    { 6, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 6, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 9, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") },
                    { 18, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 20, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 27, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") },
                    { 29, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") },
                    { 33, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 33, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") }
                });

            migrationBuilder.InsertData(
                table: "MenuItemRoles",
                columns: new[] { "MenuItemId", "RoleId" },
                values: new object[,]
                {
                    { 2, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 6, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 6, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { 9, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 29, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 33, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 33, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") }
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Exact", "Icon", "IsActive", "Label", "MenuId", "Order", "ParentId", "Route" },
                values: new object[,]
                {
                    { 7, true, "note_add", true, "Crear transacción", 1, 1, 6, "/transactions/create" },
                    { 8, true, "list_alt", true, "Consultar transacciones", 1, 2, 6, "/transactions/list" },
                    { 11, true, "download", true, "Exportar NACHA", 1, 1, 4, "/ach-cycles/nacha/export" },
                    { 12, true, "palette", true, "Identidad y colores", 1, 2, 2, "/users/branding" },
                    { 13, true, "account_balance", true, "Instituciones financieras", 1, 1, 5, "/catalogs/financial-institutions" },
                    { 14, true, "tune", true, "Prioridades cámaras", 1, 2, 5, "/catalogs/clearing-house-preferences" },
                    { 15, true, "event", true, "Festivos bancarios", 1, 3, 5, "/catalogs/bank-holidays" },
                    { 16, true, "event_busy", true, "Fechas especiales cámaras", 1, 4, 5, "/catalogs/clearing-house-special-dates" },
                    { 17, true, "upload_file", true, "Cargar NACHA-M", 1, 3, 6, "/transactions/nacha-upload" },
                    { 19, true, "schedule", true, "Tareas programadas", 1, 1, 18, "/scheduler/tasks" },
                    { 21, true, "lock", true, "Sobre digital", 1, 1, 10, "/nacha-security/sobre-digital" },
                    { 22, true, "history", true, "Auditoría", 1, 1, 27, "/audit-logs" },
                    { 23, true, "policy", true, "Reglas de contraseña", 1, 3, 2, "/users/password-rules" },
                    { 24, true, "lock_clock", true, "Bloqueo de acceso", 1, 4, 2, "/users/login-lockout" },
                    { 25, true, "fact_check", true, "Perfiles oficiales", 1, 3, 20, "/nacha-config-admin/perfiles" },
                    { 26, true, "login", true, "Autenticaciones", 1, 2, 27, "/auth-logs" },
                    { 28, true, "groups", true, "Terceros prenotificación", 1, 4, 6, "/customer-third-parties" },
                    { 30, true, "settings_ethernet", true, "Integraciones SOAP", 1, 1, 29, "/soap-integrations" },
                    { 31, true, "assignment_return", true, "Gestión devoluciones", 1, 5, 6, "/transactions/returns" },
                    { 32, true, "rule", true, "Reglas por cámara", 1, 6, 6, "/transactions/clearing-house-rules" },
                    { 34, true, "file_download", true, "Simulador NACHA-M Entrada", 1, 1, 33, "/uat/nacha-inbound-simulator" },
                    { 2802, true, "view_list", true, "Registros oficiales", 1, 4, 20, "/nacha-config-admin/records" },
                    { 2803, true, "schema", true, "Variantes y campos", 1, 5, 20, "/nacha-config-admin/variants-fields" }
                });

            migrationBuilder.InsertData(
                table: "MenuItemPermissions",
                columns: new[] { "MenuItemId", "PermissionId" },
                values: new object[,]
                {
                    { 7, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 7, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 8, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 8, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 11, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 12, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") },
                    { 13, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 14, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 15, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 16, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 19, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 22, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") },
                    { 23, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") },
                    { 24, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") },
                    { 25, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 26, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") },
                    { 28, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 28, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 30, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") },
                    { 32, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 32, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 34, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 34, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 2802, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 2803, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") }
                });

            migrationBuilder.InsertData(
                table: "MenuItemRoles",
                columns: new[] { "MenuItemId", "RoleId" },
                values: new object[,]
                {
                    { 7, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 7, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { 8, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 8, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { 12, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 13, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 13, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { 14, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 14, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { 23, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 24, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 28, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 28, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { 30, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 32, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 32, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { 34, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 34, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchBatches_AchCycleId",
                table: "AchBatches",
                column: "AchCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_AchCycles_ClearingHouseCycleConfigId",
                table: "AchCycles",
                column: "ClearingHouseCycleConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_AchCycles_ClearingHouseId_ProcessingDate_CycleName",
                table: "AchCycles",
                columns: new[] { "ClearingHouseId", "ProcessingDate", "CycleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchFileExports_AchCycleId_ExportKind_GeneratedAtUtc",
                table: "AchFileExports",
                columns: new[] { "AchCycleId", "ExportKind", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AchFileExports_ClearingHouseId",
                table: "AchFileExports",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_AchFileRejectionCodes_Code",
                table: "AchFileRejectionCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchPrenotificationPolicies_TransactionType",
                table: "AchPrenotificationPolicies",
                column: "TransactionType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchRespAttempts_Estado",
                table: "AchResponseNotificationAttempts",
                column: "EstadoNotificacion");

            migrationBuilder.CreateIndex(
                name: "IX_AchRespAttempts_FechaCreacion",
                table: "AchResponseNotificationAttempts",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "UX_AchRespAttempts_Response_Attempt",
                table: "AchResponseNotificationAttempts",
                columns: new[] { "AchResponseId", "NumeroIntento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchResponses_CorrelationId",
                table: "AchResponses",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AchResponses_EstadoProcesamiento",
                table: "AchResponses",
                column: "EstadoProcesamiento");

            migrationBuilder.CreateIndex(
                name: "IX_AchResponses_Filter",
                table: "AchResponses",
                columns: new[] { "TipoRespuesta", "CodigoCamaraCompensacion", "CodigoEstadoExterno" });

            migrationBuilder.CreateIndex(
                name: "IX_AchResponses_IdTransaccion",
                table: "AchResponses",
                column: "IdTransaccion");

            migrationBuilder.CreateIndex(
                name: "UX_AchResponses_HashIdempotencia",
                table: "AchResponses",
                column: "HashIdempotencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchRespStatusMap_Causal",
                table: "AchResponseStatusMappings",
                columns: new[] { "CodigoCamaraCompensacion", "TipoRespuesta", "CodigoEstadoExterno", "CodigoCausalExterna", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_AchRespStatusMap_Search",
                table: "AchResponseStatusMappings",
                columns: new[] { "CodigoCamaraCompensacion", "TipoRespuesta", "CodigoEstadoExterno", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_AchRespStatusMap_Vigency",
                table: "AchResponseStatusMappings",
                columns: new[] { "FechaInicioVigencia", "FechaFinVigencia" });

            migrationBuilder.CreateIndex(
                name: "IX_AchReturnCodes_ClearingHouseId_Code_FlowType_EffectiveFrom",
                table: "AchReturnCodes",
                columns: new[] { "ClearingHouseId", "Code", "FlowType", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_AchReturnOfReturnGeneratedFileAuditFlows_AchReturnOfReturnGeneratedFileAuditId_ReturnOfReturnFlowId",
                table: "AchReturnOfReturnGeneratedFileAuditFlows",
                columns: new[] { "AchReturnOfReturnGeneratedFileAuditId", "ReturnOfReturnFlowId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchReturnOfReturnGeneratedFileAuditFlows_ReturnOfReturnFlowId",
                table: "AchReturnOfReturnGeneratedFileAuditFlows",
                column: "ReturnOfReturnFlowId");

            migrationBuilder.CreateIndex(
                name: "IX_AchReturnOfReturnGeneratedFileAudits_FileName",
                table: "AchReturnOfReturnGeneratedFileAudits",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_AchReturnOfReturnPolicies_ClearingHouseId_OriginalReturnCode_Direction_FlowType_IsActive",
                table: "AchReturnOfReturnPolicies",
                columns: new[] { "ClearingHouseId", "OriginalReturnCode", "Direction", "FlowType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AchReturnPolicies_ClearingHouseId_TransactionType_Direction_FlowType_IsActive",
                table: "AchReturnPolicies",
                columns: new[] { "ClearingHouseId", "TransactionType", "Direction", "FlowType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AchReturnsGenerated_ReturnCycleId",
                table: "AchReturnsGenerated",
                column: "ReturnCycleId");

            migrationBuilder.CreateIndex(
                name: "UX_AchReturnGenerated_OriginalTransaction_Reason_Cycle",
                table: "AchReturnsGenerated",
                columns: new[] { "OriginalTransactionId", "ReturnReasonCode", "ReturnCycleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactionAddenda_AchTransactionId",
                table: "AchTransactionAddenda",
                column: "AchTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_AchBatchId",
                table: "AchTransactions",
                column: "AchBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_AchCycleId",
                table: "AchTransactions",
                column: "AchCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_CompanyEntryDescriptionId",
                table: "AchTransactions",
                column: "CompanyEntryDescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_CustomerId",
                table: "AchTransactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_DestinationInstitutionId",
                table: "AchTransactions",
                column: "DestinationInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactions_SourceInstitutionId",
                table: "AchTransactions",
                column: "SourceInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactionStateEvents_AchTransactionId",
                table: "AchTransactionStateEvents",
                column: "AchTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AchTransactionTypePolicies_TransactionType",
                table: "AchTransactionTypePolicies",
                column: "TransactionType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AddendaRecords_NachaID",
                table: "AddendaRecords",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_AuthLog_LoggedAt",
                table: "AuthLog",
                column: "LoggedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BatchControls_NachaID",
                table: "BatchControls",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_BatchHeaders_NachaID",
                table: "BatchHeaders",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_BatchNumberSequences_ClearingHouseId_OriginatingDfi_ProcessingDate_PolicyCode",
                table: "BatchNumberSequences",
                columns: new[] { "ClearingHouseId", "OriginatingDfi", "ProcessingDate", "PolicyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BulkIngestionAttempts_BatchId_AttemptNumber",
                table: "BulkIngestionAttempts",
                columns: new[] { "BatchId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BulkIngestionAttempts_BatchId_Status",
                table: "BulkIngestionAttempts",
                columns: new[] { "BatchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkIngestionBatches_BatchReference",
                table: "BulkIngestionBatches",
                column: "BatchReference");

            migrationBuilder.CreateIndex(
                name: "IX_BulkIngestionBatches_ClientRequestId_FileHash",
                table: "BulkIngestionBatches",
                columns: new[] { "ClientRequestId", "FileHash" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkIngestionBatches_UploadedAtUtc",
                table: "BulkIngestionBatches",
                column: "UploadedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BulkIngestionItems_BatchId_ItemIndex",
                table: "BulkIngestionItems",
                columns: new[] { "BatchId", "ItemIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BulkIngestionItems_BatchId_Status",
                table: "BulkIngestionItems",
                columns: new[] { "BatchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CatClearingHouse_Code",
                table: "CatClearingHouse",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatConfigStatus_Code",
                table: "CatConfigStatus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatDataSourceType_Code",
                table: "CatDataSourceType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatDirection_Code",
                table: "CatDirection",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatFlowType_Code",
                table: "CatFlowType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatFlowType_DirectionDefaultId",
                table: "CatFlowType",
                column: "DirectionDefaultId");

            migrationBuilder.CreateIndex(
                name: "IX_CatRecordCode_Code",
                table: "CatRecordCode",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatRuleType_Code",
                table: "CatRuleType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatServiceClass_ClearingHouseId",
                table: "CatServiceClass",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_CatServiceClass_Code_ClearingHouseId",
                table: "CatServiceClass",
                columns: new[] { "Code", "ClearingHouseId" },
                unique: true,
                filter: "[ClearingHouseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CenitCycleExecutions_AchCycleId",
                table: "CenitCycleExecutions",
                column: "AchCycleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CenitCycleQueues_AchTransactionId",
                table: "CenitCycleQueues",
                column: "AchTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CenitCycleQueues_CenitCycleExecutionId",
                table: "CenitCycleQueues",
                column: "CenitCycleExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CenitCycleQueues_TargetAchCycleId_Status",
                table: "CenitCycleQueues",
                columns: new[] { "TargetAchCycleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CenitNetPositions_CenitNettingExecutionId_FinancialInstitutionId",
                table: "CenitNetPositions",
                columns: new[] { "CenitNettingExecutionId", "FinancialInstitutionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CenitNetPositions_FinancialInstitutionId",
                table: "CenitNetPositions",
                column: "FinancialInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CenitNettingDetails_AchTransactionId",
                table: "CenitNettingDetails",
                column: "AchTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CenitNettingDetails_CenitNettingExecutionId",
                table: "CenitNettingDetails",
                column: "CenitNettingExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CenitNettingExecutions_CenitCycleExecutionId",
                table: "CenitNettingExecutions",
                column: "CenitCycleExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateLoadAudits_CertificateVersionId",
                table: "CertificateLoadAudits",
                column: "CertificateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateLoadAudits_LoadedAtUtc",
                table: "CertificateLoadAudits",
                column: "LoadedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRotationHistories_NewVersionId",
                table: "CertificateRotationHistories",
                column: "NewVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRotationHistories_PreviousVersionId_NewVersionId",
                table: "CertificateRotationHistories",
                columns: new[] { "PreviousVersionId", "NewVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateUsageLogs_CertificateVersionId",
                table: "CertificateUsageLogs",
                column: "CertificateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateUsageLogs_OccurredAtUtc",
                table: "CertificateUsageLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateUsageLogs_Result",
                table: "CertificateUsageLogs",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_CfgFieldRule_LayoutFieldId_Order",
                table: "CfgFieldRule",
                columns: new[] { "LayoutFieldId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CfgFieldRule_LayoutFieldId_RuleCode",
                table: "CfgFieldRule",
                columns: new[] { "LayoutFieldId", "RuleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CfgFieldRule_RuleTypeId",
                table: "CfgFieldRule",
                column: "RuleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CfgFieldSourceDefinition_DataSourceTypeId",
                table: "CfgFieldSourceDefinition",
                column: "DataSourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CfgLayoutField_LayoutVariantId_FieldCode",
                table: "CfgLayoutField",
                columns: new[] { "LayoutVariantId", "FieldCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CfgLayoutField_LayoutVariantId_SortOrder",
                table: "CfgLayoutField",
                columns: new[] { "LayoutVariantId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CfgLayoutField_LayoutVariantId_StartPosition",
                table: "CfgLayoutField",
                columns: new[] { "LayoutVariantId", "StartPosition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CfgLayoutField_SourceDefinitionId",
                table: "CfgLayoutField",
                column: "SourceDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CfgLayoutVariant_ProfileId_RecordCodeId_StatusId_EffectiveFrom_EffectiveTo_Priority",
                table: "CfgLayoutVariant",
                columns: new[] { "ProfileId", "RecordCodeId", "StatusId", "EffectiveFrom", "EffectiveTo", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_CfgLayoutVariant_ProfileId_RecordCodeId_VariantCode",
                table: "CfgLayoutVariant",
                columns: new[] { "ProfileId", "RecordCodeId", "VariantCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CfgLayoutVariant_RecordCodeId",
                table: "CfgLayoutVariant",
                column: "RecordCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CfgLayoutVariant_StatusId",
                table: "CfgLayoutVariant",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfile_ClearingHouseId_FlowTypeId_DirectionId_ServiceClassId_StatusId_ContextPriority",
                table: "CfgProfile",
                columns: new[] { "ClearingHouseId", "FlowTypeId", "DirectionId", "ServiceClassId", "StatusId", "ContextPriority" });

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfile_ClearingHouseId_FlowTypeId_DirectionId_ServiceClassId_VersionMajor_VersionMinor",
                table: "CfgProfile",
                columns: new[] { "ClearingHouseId", "FlowTypeId", "DirectionId", "ServiceClassId", "VersionMajor", "VersionMinor" },
                unique: true,
                filter: "[ServiceClassId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfile_DirectionId",
                table: "CfgProfile",
                column: "DirectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfile_FlowTypeId",
                table: "CfgProfile",
                column: "FlowTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfile_ProfileCode",
                table: "CfgProfile",
                column: "ProfileCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfile_ServiceClassId",
                table: "CfgProfile",
                column: "ServiceClassId");

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfile_StatusId_EffectiveFrom_EffectiveTo",
                table: "CfgProfile",
                columns: new[] { "StatusId", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfile_SupersedesProfileId",
                table: "CfgProfile",
                column: "SupersedesProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfileRecord_LayoutVariantId",
                table: "CfgProfileRecord",
                column: "LayoutVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfileRecord_ProfileId_RecordCodeId_Sequence",
                table: "CfgProfileRecord",
                columns: new[] { "ProfileId", "RecordCodeId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfileRecord_ProfileId_Sequence",
                table: "CfgProfileRecord",
                columns: new[] { "ProfileId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfileRecord_RecordCodeId",
                table: "CfgProfileRecord",
                column: "RecordCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfileRecord_SemanticRuleSetId",
                table: "CfgProfileRecord",
                column: "SemanticRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_CfgProfileTag_ProfileId_TagKey_TagValue",
                table: "CfgProfileTag",
                columns: new[] { "ProfileId", "TagKey", "TagValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CfgPublishRequest_ProfileId_Status_RequestedAt",
                table: "CfgPublishRequest",
                columns: new[] { "ProfileId", "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CfgRuleSet_RuleSetCode",
                table: "CfgRuleSet",
                column: "RuleSetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CfgRuleSetRule_RuleSetId_Order",
                table: "CfgRuleSetRule",
                columns: new[] { "RuleSetId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CfgRuleSetRule_RuleSetId_RuleCode",
                table: "CfgRuleSetRule",
                columns: new[] { "RuleSetId", "RuleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CfgRuleSetRule_RuleTypeId",
                table: "CfgRuleSetRule",
                column: "RuleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClearingHouseConfigs_ClearingHouseId",
                table: "ClearingHouseConfigs",
                column: "ClearingHouseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClearingHouseCycleConfigs_ClearingHouseId",
                table: "ClearingHouseCycleConfigs",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClearingHouses_ClearingHouseId",
                table: "ClearingHouses",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClearingHouses_Code",
                table: "ClearingHouses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClearingHouseSpecialDates_ClearingHouseId_Date",
                table: "ClearingHouseSpecialDates",
                columns: new[] { "ClearingHouseId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CHTR_RuleLookup",
                table: "ClearingHouseTransactionRules",
                columns: new[] { "ClearingHouseId", "TransactionNature", "TransactionType", "AppliesToNachaExport", "AppliesToMonetaryTransactions", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyEntryDescription_Term",
                table: "CompanyEntryDescription",
                column: "Term",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchAttempts_DispatchBatchId_CreatedAt",
                table: "ContrapartidaDispatchAttempts",
                columns: new[] { "DispatchBatchId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchAttempts_DispatchItemId_AttemptNumber",
                table: "ContrapartidaDispatchAttempts",
                columns: new[] { "DispatchItemId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchAttempts_Result",
                table: "ContrapartidaDispatchAttempts",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchBatches_AchBatchId",
                table: "ContrapartidaDispatchBatches",
                column: "AchBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchBatches_AchCycleId",
                table: "ContrapartidaDispatchBatches",
                column: "AchCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchBatches_ClearingHouseId_AchCycleId_TriggeredAtUtc",
                table: "ContrapartidaDispatchBatches",
                columns: new[] { "ClearingHouseId", "AchCycleId", "TriggeredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchBatches_MappingSetId_MappingVersion",
                table: "ContrapartidaDispatchBatches",
                columns: new[] { "MappingSetId", "MappingVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchBatches_Status",
                table: "ContrapartidaDispatchBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchItems_AchBatchId",
                table: "ContrapartidaDispatchItems",
                column: "AchBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchItems_AchCycleId",
                table: "ContrapartidaDispatchItems",
                column: "AchCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchItems_AchTransactionId",
                table: "ContrapartidaDispatchItems",
                column: "AchTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchItems_ClearingHouseId_AchCycleId_State",
                table: "ContrapartidaDispatchItems",
                columns: new[] { "ClearingHouseId", "AchCycleId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_ContrapartidaDispatchItems_State_NextAttemptAtUtc",
                table: "ContrapartidaDispatchItems",
                columns: new[] { "State", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAccounts_CustomerId_AccountNumber",
                table: "CustomerAccounts",
                columns: new[] { "CustomerId", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_AddressType",
                table: "CustomerAddresses",
                column: "AddressType");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_CustomerId",
                table: "CustomerAddresses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerEmails_CustomerId_IsPrimary",
                table: "CustomerEmails",
                columns: new[] { "CustomerId", "IsPrimary" },
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerEmails_EmailType",
                table: "CustomerEmails",
                column: "EmailType");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPhones_CustomerId_IsPrimary",
                table: "CustomerPhones",
                columns: new[] { "CustomerId", "IsPrimary" },
                unique: true,
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPhones_PhoneType",
                table: "CustomerPhones",
                column: "PhoneType");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_DocumentType_DocumentNumber",
                table: "Customers",
                columns: new[] { "DocumentType", "DocumentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Gender",
                table: "Customers",
                column: "Gender");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PersonType",
                table: "Customers",
                column: "PersonType");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerThirdParties_CustomerId_DestinationInstitutionId_DestinationAccountNumber_RecipientIdNumber",
                table: "CustomerThirdParties",
                columns: new[] { "CustomerId", "DestinationInstitutionId", "DestinationAccountNumber", "RecipientIdNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerThirdParties_DestinationInstitutionId",
                table: "CustomerThirdParties",
                column: "DestinationInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerThirdParties_PrenotificationTransactionId",
                table: "CustomerThirdParties",
                column: "PrenotificationTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificates_Code",
                table: "DigitalCertificates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_DigitalCertificateId",
                table: "DigitalCertificateVersions",
                column: "DigitalCertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_NotAfter",
                table: "DigitalCertificateVersions",
                column: "NotAfter");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_ReplacedByVersionId",
                table: "DigitalCertificateVersions",
                column: "ReplacedByVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_SerialNumber",
                table: "DigitalCertificateVersions",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalCertificateVersions_Thumbprint",
                table: "DigitalCertificateVersions",
                column: "Thumbprint");

            migrationBuilder.CreateIndex(
                name: "UX_DCV_Active_Context",
                table: "DigitalCertificateVersions",
                columns: new[] { "ClearingHouseId", "Environment", "Purpose", "HolderType" },
                unique: true,
                filter: "[Status] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalEnvelopeOperationLogs_CertificateVersionId",
                table: "DigitalEnvelopeOperationLogs",
                column: "CertificateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalEnvelopeOperationLogs_OccurredAtUtc_ClearingHouseId_Environment_Purpose_Result",
                table: "DigitalEnvelopeOperationLogs",
                columns: new[] { "OccurredAtUtc", "ClearingHouseId", "Environment", "Purpose", "Result" });

            migrationBuilder.CreateIndex(
                name: "IX_EntryDetails_NachaID",
                table: "EntryDetails",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFileNameRegistry_ClearingHouseId_CycleId_ExternalFileType_CreatedAtUtc",
                table: "ExternalFileNameRegistry",
                columns: new[] { "ClearingHouseId", "CycleId", "ExternalFileType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFileNameRegistry_ClearingHouseId_ExternalFileName_ProcessingDate",
                table: "ExternalFileNameRegistry",
                columns: new[] { "ClearingHouseId", "ExternalFileName", "ProcessingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFileNameValidationLog_RegistryId_CreatedAtUtc",
                table: "ExternalFileNameValidationLog",
                columns: new[] { "RegistryId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalFileSequences_ClearingHouseId_ScopeCode_SequenceDate",
                table: "ExternalFileSequences",
                columns: new[] { "ClearingHouseId", "ScopeCode", "SequenceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileControls_NachaID",
                table: "FileControls",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInstitutions_ClearingHouseId",
                table: "FinancialInstitutions",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_HistConfigChange_CorrelationId",
                table: "HistConfigChange",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_HistConfigChange_ProfileId_ChangedAtUtc",
                table: "HistConfigChange",
                columns: new[] { "ProfileId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HistConfigSnapshot_ProfileId_VersionMajor_VersionMinor_CreatedAtUtc",
                table: "HistConfigSnapshot",
                columns: new[] { "ProfileId", "VersionMajor", "VersionMinor", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaDispatchQueue_AchTransactionId",
                table: "IncomingNachaDispatchQueue",
                column: "AchTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaDispatchQueue_IdempotencyDispatchKey",
                table: "IncomingNachaDispatchQueue",
                column: "IdempotencyDispatchKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaDispatchQueue_IncomingNachaEntryClassificationId",
                table: "IncomingNachaDispatchQueue",
                column: "IncomingNachaEntryClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaDispatchQueue_IncomingNachaFileIngestionId",
                table: "IncomingNachaDispatchQueue",
                column: "IncomingNachaFileIngestionId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaDispatchQueue_IncomingNachaTransactionLinkId",
                table: "IncomingNachaDispatchQueue",
                column: "IncomingNachaTransactionLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaDispatchQueue_QueueStatus_NextAttemptAtUtc_Priority",
                table: "IncomingNachaDispatchQueue",
                columns: new[] { "QueueStatus", "NextAttemptAtUtc", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaEntryClassifications_AddendaRecordId",
                table: "IncomingNachaEntryClassifications",
                column: "AddendaRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaEntryClassifications_EntryDetailId",
                table: "IncomingNachaEntryClassifications",
                column: "EntryDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaEntryClassifications_FunctionalClass_EligibilityStatus",
                table: "IncomingNachaEntryClassifications",
                columns: new[] { "FunctionalClass", "EligibilityStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaEntryClassifications_IncomingNachaFileIngestionId_EntryDetailId_AddendaRecordId",
                table: "IncomingNachaEntryClassifications",
                columns: new[] { "IncomingNachaFileIngestionId", "EntryDetailId", "AddendaRecordId" },
                unique: true,
                filter: "[AddendaRecordId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaFileIngestions_CorrelationId",
                table: "IncomingNachaFileIngestions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaFileIngestions_FileHashSha256_FileSize_IsReprocess_ParentIngestionId",
                table: "IncomingNachaFileIngestions",
                columns: new[] { "FileHashSha256", "FileSize", "IsReprocess", "ParentIngestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaFileIngestions_ParentIngestionId",
                table: "IncomingNachaFileIngestions",
                column: "ParentIngestionId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaFileIngestions_ResolvedClearingHouseId_OperationalDate_ResolvedAchCycleId",
                table: "IncomingNachaFileIngestions",
                columns: new[] { "ResolvedClearingHouseId", "OperationalDate", "ResolvedAchCycleId" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaFileIngestions_UploadedAtUtc_FileName",
                table: "IncomingNachaFileIngestions",
                columns: new[] { "UploadedAtUtc", "FileName" });

            migrationBuilder.CreateIndex(
                name: "UX_IncomingNachaFileIngestions_FileHash_FileSize_Canonical",
                table: "IncomingNachaFileIngestions",
                columns: new[] { "FileHashSha256", "FileSize" },
                unique: true,
                filter: "\"IsReprocess\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaFileProcessingResults_IncomingNachaFileIngestionId_AttemptNumber",
                table: "IncomingNachaFileProcessingResults",
                columns: new[] { "IncomingNachaFileIngestionId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaFileProcessingResults_StartedAtUtc",
                table: "IncomingNachaFileProcessingResults",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaIntegrationExecution_CorrelationId",
                table: "IncomingNachaIntegrationExecution",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaIntegrationExecution_DispatchQueueId_StartedAtUtc",
                table: "IncomingNachaIntegrationExecution",
                columns: new[] { "DispatchQueueId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaProcessingEvents_EventType",
                table: "IncomingNachaProcessingEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaProcessingEvents_IncomingNachaFileIngestionId_OccurredAtUtc",
                table: "IncomingNachaProcessingEvents",
                columns: new[] { "IncomingNachaFileIngestionId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaProcessingEvents_IncomingNachaFileIngestionId1",
                table: "IncomingNachaProcessingEvents",
                column: "IncomingNachaFileIngestionId1");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaTransactionLinks_AchTransactionId",
                table: "IncomingNachaTransactionLinks",
                column: "AchTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaTransactionLinks_AddendaRecordId",
                table: "IncomingNachaTransactionLinks",
                column: "AddendaRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaTransactionLinks_EntryDetailId",
                table: "IncomingNachaTransactionLinks",
                column: "EntryDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomingNachaTransactionLinks_IncomingNachaFileIngestionId_EntryDetailId_AddendaRecordId_AchTransactionId",
                table: "IncomingNachaTransactionLinks",
                columns: new[] { "IncomingNachaFileIngestionId", "EntryDetailId", "AddendaRecordId", "AchTransactionId" });

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionClearingHousePreferences_ClearingHouseId",
                table: "InstitutionClearingHousePreferences",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionClearingHousePreferences_FinancialInstitutionId_ClearingHouseId",
                table: "InstitutionClearingHousePreferences",
                columns: new[] { "FinancialInstitutionId", "ClearingHouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingRules_MappingSetId_ParameterId_Priority",
                table: "IntegrationMappingRules",
                columns: new[] { "MappingSetId", "ParameterId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingRules_ParameterId",
                table: "IntegrationMappingRules",
                column: "ParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingRules_SourceCatalogFieldId",
                table: "IntegrationMappingRules",
                column: "SourceCatalogFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingSetHistory_MappingSetId_PerformedAtUtc",
                table: "IntegrationMappingSetHistory",
                columns: new[] { "MappingSetId", "PerformedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingSets_MethodId_Status_Version",
                table: "IntegrationMappingSets",
                columns: new[] { "MethodId", "Status", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingTraceEntries_TraceId_TargetField",
                table: "IntegrationMappingTraceEntries",
                columns: new[] { "TraceId", "TargetField" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingTraces_CorrelationId",
                table: "IntegrationMappingTraces",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingTraces_IntegrationKey_OperationKey_CreatedAtUtc",
                table: "IntegrationMappingTraces",
                columns: new[] { "IntegrationKey", "OperationKey", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMappingTraces_TransactionId",
                table: "IntegrationMappingTraces",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMethodParameters_MethodId_ParameterPath",
                table: "IntegrationMethodParameters",
                columns: new[] { "MethodId", "ParameterPath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMethods_Code",
                table: "IntegrationMethods",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationSourceCatalogFields_MethodId_SourceKind_FieldPath",
                table: "IntegrationSourceCatalogFields",
                columns: new[] { "MethodId", "SourceKind", "FieldPath" },
                unique: true,
                filter: "[MethodId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LiquidityOptimizationDecisions_AchTransactionId",
                table: "LiquidityOptimizationDecisions",
                column: "AchTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_LiquidityOptimizationDecisions_CenitCycleExecutionId_DecisionType",
                table: "LiquidityOptimizationDecisions",
                columns: new[] { "CenitCycleExecutionId", "DecisionType" });

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

            migrationBuilder.CreateIndex(
                name: "IX_NachaFileIdentifierMap_Sequence",
                table: "NachaFileIdentifierMap",
                column: "Sequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NachaFileNamingRules_SourceFinancialInstitutionId",
                table: "NachaFileNamingRules",
                column: "SourceFinancialInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_NFNR_RuleLookup",
                table: "NachaFileNamingRules",
                columns: new[] { "ClearingHouseId", "FileDirection", "IsActive", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_NachaHeaders_AchCycleId",
                table: "NachaHeaders",
                column: "AchCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_NachaHeaders_ClearingHouseId",
                table: "NachaHeaders",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_NachaHeaders_IncomingNachaFileIngestionId",
                table: "NachaHeaders",
                column: "IncomingNachaFileIngestionId");

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

            migrationBuilder.CreateIndex(
                name: "IX_NachaRecordFields_NachaRecordLayoutId",
                table: "NachaRecordFields",
                column: "NachaRecordLayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_NachaSecurityOperations_OperationId",
                table: "NachaSecurityOperations",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NachaSecurityOperations_RequestedAtUtc_OperationType_Status",
                table: "NachaSecurityOperations",
                columns: new[] { "RequestedAtUtc", "OperationType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NavigationLog_Route",
                table: "NavigationLog",
                column: "Route");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationLog_UserId",
                table: "NavigationLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NavigationLog_VisitedAt",
                table: "NavigationLog",
                column: "VisitedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_Token",
                table: "PasswordResetTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRailCapabilityRegistry_RailCode_CapabilityCode_IsActive_EffectiveFromUtc",
                table: "PaymentRailCapabilityRegistry",
                columns: new[] { "RailCode", "CapabilityCode", "IsActive", "EffectiveFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOfReturnFlows_CenitCycleExecutionId",
                table: "ReturnOfReturnFlows",
                column: "CenitCycleExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOfReturnFlows_ReturnOfReturnTransactionId",
                table: "ReturnOfReturnFlows",
                column: "ReturnOfReturnTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnOfReturnFlows_SourceReturnTransactionId",
                table: "ReturnOfReturnFlows",
                column: "SourceReturnTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnReasons_Code",
                table: "ReturnReasons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchFileExports");

            migrationBuilder.DropTable(
                name: "AchFileRejectionCodes");

            migrationBuilder.DropTable(
                name: "AchPrenotificationPolicies");

            migrationBuilder.DropTable(
                name: "AchResponseNotificationAttempts");

            migrationBuilder.DropTable(
                name: "AchResponseStatusMappings");

            migrationBuilder.DropTable(
                name: "AchReturnCodes");

            migrationBuilder.DropTable(
                name: "AchReturnOfReturnGeneratedFileAuditFlows");

            migrationBuilder.DropTable(
                name: "AchReturnOfReturnPolicies");

            migrationBuilder.DropTable(
                name: "AchReturnPolicies");

            migrationBuilder.DropTable(
                name: "AchReturnsGenerated");

            migrationBuilder.DropTable(
                name: "AchTransactionAddenda");

            migrationBuilder.DropTable(
                name: "AchTransactionStateEvents");

            migrationBuilder.DropTable(
                name: "AchTransactionTypePolicies");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "AuthLog");

            migrationBuilder.DropTable(
                name: "BankHolidays");

            migrationBuilder.DropTable(
                name: "BatchControls");

            migrationBuilder.DropTable(
                name: "BatchHeaders");

            migrationBuilder.DropTable(
                name: "BatchNumberSequences");

            migrationBuilder.DropTable(
                name: "BrandingSettings");

            migrationBuilder.DropTable(
                name: "BulkIngestionAttempts");

            migrationBuilder.DropTable(
                name: "BulkIngestionItems");

            migrationBuilder.DropTable(
                name: "CenitCycleQueues");

            migrationBuilder.DropTable(
                name: "CenitNetPositions");

            migrationBuilder.DropTable(
                name: "CenitNettingDetails");

            migrationBuilder.DropTable(
                name: "CertificateLoadAudits");

            migrationBuilder.DropTable(
                name: "CertificateRotationHistories");

            migrationBuilder.DropTable(
                name: "CertificateUsageLogs");

            migrationBuilder.DropTable(
                name: "CfgFieldRule");

            migrationBuilder.DropTable(
                name: "CfgProfileRecord");

            migrationBuilder.DropTable(
                name: "CfgProfileTag");

            migrationBuilder.DropTable(
                name: "CfgPublishRequest");

            migrationBuilder.DropTable(
                name: "CfgRuleSetRule");

            migrationBuilder.DropTable(
                name: "ClearingHouseSpecialDates");

            migrationBuilder.DropTable(
                name: "ClearingHouseTransactionRules");

            migrationBuilder.DropTable(
                name: "CompanyEntryDescription");

            migrationBuilder.DropTable(
                name: "ContrapartidaDispatchAttempts");

            migrationBuilder.DropTable(
                name: "CustomerAccounts");

            migrationBuilder.DropTable(
                name: "CustomerAddresses");

            migrationBuilder.DropTable(
                name: "CustomerEmails");

            migrationBuilder.DropTable(
                name: "CustomerPhones");

            migrationBuilder.DropTable(
                name: "CustomerThirdParties");

            migrationBuilder.DropTable(
                name: "DigitalEnvelopeCertificates");

            migrationBuilder.DropTable(
                name: "DigitalEnvelopeOperationLogs");

            migrationBuilder.DropTable(
                name: "ExternalFileNameValidationLog");

            migrationBuilder.DropTable(
                name: "ExternalFileSequences");

            migrationBuilder.DropTable(
                name: "FileControls");

            migrationBuilder.DropTable(
                name: "HistConfigChange");

            migrationBuilder.DropTable(
                name: "HistConfigSnapshot");

            migrationBuilder.DropTable(
                name: "IncomingNachaFileProcessingResults");

            migrationBuilder.DropTable(
                name: "IncomingNachaIntegrationExecution");

            migrationBuilder.DropTable(
                name: "IncomingNachaProcessingEvents");

            migrationBuilder.DropTable(
                name: "InstitutionClearingHousePreferences");

            migrationBuilder.DropTable(
                name: "IntegrationMappingRules");

            migrationBuilder.DropTable(
                name: "IntegrationMappingSetHistory");

            migrationBuilder.DropTable(
                name: "IntegrationMappingTraceEntries");

            migrationBuilder.DropTable(
                name: "LiquidityOptimizationDecisions");

            migrationBuilder.DropTable(
                name: "LoginLockoutSettings");

            migrationBuilder.DropTable(
                name: "MenuItemPermissions");

            migrationBuilder.DropTable(
                name: "MenuItemRoles");

            migrationBuilder.DropTable(
                name: "NachaFileIdentifierMap");

            migrationBuilder.DropTable(
                name: "NachaFileNamingRules");

            migrationBuilder.DropTable(
                name: "NachaInboundSimulationEntries");

            migrationBuilder.DropTable(
                name: "NachaRecordDefinitions");

            migrationBuilder.DropTable(
                name: "NachaRecordFields");

            migrationBuilder.DropTable(
                name: "NachaSecurityOperations");

            migrationBuilder.DropTable(
                name: "NavigationLog");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "PasswordRuleSettings");

            migrationBuilder.DropTable(
                name: "PaymentRailCapabilityRegistry");

            migrationBuilder.DropTable(
                name: "ReturnReasons");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SoapIntegrationSettings");

            migrationBuilder.DropTable(
                name: "TaskExecutionLog");

            migrationBuilder.DropTable(
                name: "TaskParameters");

            migrationBuilder.DropTable(
                name: "TransactionCodes");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "AchResponses");

            migrationBuilder.DropTable(
                name: "AchReturnOfReturnGeneratedFileAudits");

            migrationBuilder.DropTable(
                name: "ReturnOfReturnFlows");

            migrationBuilder.DropTable(
                name: "BulkIngestionBatches");

            migrationBuilder.DropTable(
                name: "CenitNettingExecutions");

            migrationBuilder.DropTable(
                name: "CfgLayoutField");

            migrationBuilder.DropTable(
                name: "CatRuleType");

            migrationBuilder.DropTable(
                name: "CfgRuleSet");

            migrationBuilder.DropTable(
                name: "ContrapartidaDispatchBatches");

            migrationBuilder.DropTable(
                name: "ContrapartidaDispatchItems");

            migrationBuilder.DropTable(
                name: "AddressTypes");

            migrationBuilder.DropTable(
                name: "EmailTypes");

            migrationBuilder.DropTable(
                name: "PhoneTypes");

            migrationBuilder.DropTable(
                name: "DigitalCertificateVersions");

            migrationBuilder.DropTable(
                name: "ExternalFileNameRegistry");

            migrationBuilder.DropTable(
                name: "IncomingNachaDispatchQueue");

            migrationBuilder.DropTable(
                name: "IntegrationMethodParameters");

            migrationBuilder.DropTable(
                name: "IntegrationSourceCatalogFields");

            migrationBuilder.DropTable(
                name: "IntegrationMappingSets");

            migrationBuilder.DropTable(
                name: "IntegrationMappingTraces");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "NachaInboundSimulations");

            migrationBuilder.DropTable(
                name: "NachaRecordLayouts");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "TaskDefinition");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "CenitCycleExecutions");

            migrationBuilder.DropTable(
                name: "CfgFieldSourceDefinition");

            migrationBuilder.DropTable(
                name: "CfgLayoutVariant");

            migrationBuilder.DropTable(
                name: "DigitalCertificates");

            migrationBuilder.DropTable(
                name: "IncomingNachaEntryClassifications");

            migrationBuilder.DropTable(
                name: "IncomingNachaTransactionLinks");

            migrationBuilder.DropTable(
                name: "IntegrationMethods");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropTable(
                name: "CatDataSourceType");

            migrationBuilder.DropTable(
                name: "CatRecordCode");

            migrationBuilder.DropTable(
                name: "CfgProfile");

            migrationBuilder.DropTable(
                name: "AchTransactions");

            migrationBuilder.DropTable(
                name: "AddendaRecords");

            migrationBuilder.DropTable(
                name: "EntryDetails");

            migrationBuilder.DropTable(
                name: "CatConfigStatus");

            migrationBuilder.DropTable(
                name: "CatFlowType");

            migrationBuilder.DropTable(
                name: "CatServiceClass");

            migrationBuilder.DropTable(
                name: "AchBatches");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "FinancialInstitutions");

            migrationBuilder.DropTable(
                name: "NachaHeaders");

            migrationBuilder.DropTable(
                name: "CatDirection");

            migrationBuilder.DropTable(
                name: "CatClearingHouse");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "GenderTypes");

            migrationBuilder.DropTable(
                name: "PersonTypes");

            migrationBuilder.DropTable(
                name: "AchCycles");

            migrationBuilder.DropTable(
                name: "IncomingNachaFileIngestions");

            migrationBuilder.DropTable(
                name: "ClearingHouseCycleConfigs");

            migrationBuilder.DropTable(
                name: "ClearingHouses");

            migrationBuilder.DropTable(
                name: "ClearingHouseConfigs");
        }
    }
}
