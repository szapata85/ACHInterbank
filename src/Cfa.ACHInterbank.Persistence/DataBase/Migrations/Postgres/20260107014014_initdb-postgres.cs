using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class initdbpostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ChangedFields = table.Column<string>(type: "text", nullable: true),
                    BeforeJson = table.Column<string>(type: "text", nullable: true),
                    AfterJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankHolidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CountryCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BrandingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicLogo = table.Column<string>(type: "text", nullable: true),
                    PrivateLogo = table.Column<string>(type: "text", nullable: true),
                    PublicBackground = table.Column<string>(type: "text", nullable: true),
                    PrivateBackground = table.Column<string>(type: "text", nullable: true),
                    SidebarBackground = table.Column<string>(type: "text", nullable: true),
                    ButtonColor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandingSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClearingHouseConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    HolidayStrategy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClearingHouseConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SecondLastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DocumentType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DigitalEnvelopeCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    RawData = table.Column<byte[]>(type: "bytea", nullable: false),
                    Password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HasPrivateKey = table.Column<bool>(type: "boolean", nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Issuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Thumbprint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NotBefore = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotAfter = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalEnvelopeCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NachaRecordLayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecordType = table.Column<string>(type: "text", nullable: false),
                    RecordCode = table.Column<string>(type: "text", nullable: false),
                    TotalLength = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NachaRecordLayouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordRuleSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MinLength = table.Column<int>(type: "integer", nullable: false),
                    MinUppercase = table.Column<int>(type: "integer", nullable: false),
                    MinNumbers = table.Column<int>(type: "integer", nullable: false),
                    MinSpecial = table.Column<int>(type: "integer", nullable: false),
                    MaxSpecial = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordRuleSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskDefinition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CalendarPolicy = table.Column<int>(type: "integer", nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConcurrencyPolicy = table.Column<int>(type: "integer", nullable: false),
                    RetryOnFailure = table.Column<bool>(type: "boolean", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: true),
                    RetryBackoffSeconds = table.Column<int>(type: "integer", nullable: false),
                    PeriodicityType = table.Column<int>(type: "integer", nullable: false),
                    N = table.Column<int>(type: "integer", nullable: true),
                    Minute = table.Column<int>(type: "integer", nullable: true),
                    TimeOfDayTicks = table.Column<long>(type: "bigint", nullable: true),
                    WeeklyDay = table.Column<int>(type: "integer", nullable: true),
                    MonthDay = table.Column<int>(type: "integer", nullable: true),
                    CronExpression = table.Column<string>(type: "text", nullable: true),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDefinition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClearingHouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OriginCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false)
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
                name: "CustomerAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    AddressType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Colombia"),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddresses", x => x.Id);
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    EmailType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "CustomerPhones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    PhoneType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MenuId = table.Column<int>(type: "integer", nullable: true),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Route = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Exact = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NachaRecordLayoutId = table.Column<int>(type: "integer", nullable: false),
                    FieldName = table.Column<string>(type: "text", nullable: false),
                    StartPosition = table.Column<int>(type: "integer", nullable: false),
                    Length = table.Column<int>(type: "integer", nullable: false),
                    PadChar = table.Column<char>(type: "character(1)", nullable: false),
                    Justification = table.Column<char>(type: "character(1)", nullable: false),
                    DbColumn = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: true)
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
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaskDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    Output = table.Column<string>(type: "text", nullable: true),
                    ExecutionKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaskDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Expiration = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
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
                name: "AchCycles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CycleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProcessingDate = table.Column<DateTime>(type: "date", nullable: false),
                    CutoffTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    RescheduleOnHoliday = table.Column<bool>(type: "boolean", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AchCycles_ClearingHouses_ClearingHouseId",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClearingHouseCycleConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    CycleName = table.Column<string>(type: "text", nullable: false),
                    CutoffTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
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
                name: "FinancialInstitutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDefaultSource = table.Column<bool>(type: "boolean", nullable: false),
                    RoutingNumber = table.Column<string>(type: "text", nullable: false),
                    TransitCode = table.Column<string>(type: "text", nullable: false),
                    CheckDigit = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "MenuItemPermissions",
                columns: table => new
                {
                    MenuItemId = table.Column<int>(type: "integer", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    MenuItemId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "AchBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AchCycleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ServiceClassCode = table.Column<string>(type: "text", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    CompanyIdentification = table.Column<string>(type: "text", nullable: false),
                    CompanyEntryDescription = table.Column<string>(type: "text", nullable: false),
                    OriginOrOdfi = table.Column<string>(type: "text", nullable: false),
                    EffectiveEntryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BatchSequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    TotalDebitAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalCreditAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "NachaHeaders",
                columns: table => new
                {
                    NachaID = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PriorityCode = table.Column<string>(type: "text", nullable: true),
                    ImmediateDestination = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ImmediateOrigin = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    FileCreationDate = table.Column<string>(type: "text", nullable: true),
                    FileCreationTime = table.Column<string>(type: "text", nullable: true),
                    FileIdModifier = table.Column<string>(type: "text", nullable: true),
                    RecordSize = table.Column<string>(type: "text", nullable: true),
                    BlockingFactor = table.Column<string>(type: "text", nullable: true),
                    FormatCode = table.Column<string>(type: "text", nullable: true),
                    ImmediateDestinationName = table.Column<string>(type: "text", nullable: true),
                    ImmediateOriginName = table.Column<string>(type: "text", nullable: true),
                    ReferenceCode = table.Column<string>(type: "text", nullable: true),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: true),
                    CycleNumber = table.Column<int>(type: "integer", nullable: false),
                    AchCycleId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "InstitutionClearingHousePreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FinancialInstitutionId = table.Column<int>(type: "integer", nullable: false),
                    ClearingHouseId = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionClearingHousePreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstitutionClearingHousePreferences_ClearingHouses_Clearing~",
                        column: x => x.ClearingHouseId,
                        principalTable: "ClearingHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InstitutionClearingHousePreferences_FinancialInstitutions_F~",
                        column: x => x.FinancialInstitutionId,
                        principalTable: "FinancialInstitutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AchTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    TransactionCode = table.Column<string>(type: "text", nullable: false),
                    ServiceClassCode = table.Column<string>(type: "text", nullable: false),
                    CompanyEntryDescription = table.Column<string>(type: "text", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    CompanyIdentification = table.Column<string>(type: "text", nullable: false),
                    OriginatingDFI = table.Column<string>(type: "text", nullable: false),
                    ReceivingDFI = table.Column<string>(type: "text", nullable: false),
                    TraceNumber = table.Column<string>(type: "text", nullable: false),
                    TraceSequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    EffectiveEntryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AddendaRecordIndicator = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrenotification = table.Column<bool>(type: "boolean", nullable: false),
                    RecipientIdNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DiscretionaryData = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    SourceAccountNumber = table.Column<string>(type: "text", nullable: false),
                    DestinationAccountNumber = table.Column<string>(type: "text", nullable: false),
                    SourceInstitutionId = table.Column<int>(type: "integer", nullable: false),
                    DestinationInstitutionId = table.Column<int>(type: "integer", nullable: false),
                    AchCycleId = table.Column<string>(type: "character varying(40)", nullable: false),
                    AchBatchId = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                        name: "FK_AchTransactions_FinancialInstitutions_DestinationInstitutio~",
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
                name: "AddendaRecords",
                columns: table => new
                {
                    AddendaID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodeTypeAddendumRecord = table.Column<string>(type: "text", nullable: true),
                    IdUserOrig = table.Column<string>(type: "text", nullable: true),
                    PurposeOfTransaction = table.Column<string>(type: "text", nullable: true),
                    InvoiceOrAccountNumber = table.Column<string>(type: "text", nullable: true),
                    InfofromOriginator = table.Column<string>(type: "text", nullable: true),
                    AddendumSequence = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    EntryDetailSequenceNumber = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    NachaID = table.Column<string>(type: "character varying(64)", nullable: true)
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
                    BatchControlID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchTranClassCode = table.Column<string>(type: "text", nullable: true),
                    EntryAddendaCount = table.Column<int>(type: "integer", nullable: true),
                    TotalEntry = table.Column<int>(type: "integer", nullable: true),
                    TotalDebitAmount = table.Column<decimal>(type: "money", nullable: false),
                    TotalCreditAmount = table.Column<decimal>(type: "money", nullable: false),
                    IdUserOrig = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CodAutMessage = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    IdOrigEntity = table.Column<string>(type: "text", nullable: true),
                    BatchNumber = table.Column<string>(type: "text", nullable: true),
                    NachaID = table.Column<string>(type: "character varying(64)", nullable: true)
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
                    BatchID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceClassCode = table.Column<string>(type: "text", nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    DiscretionaryData = table.Column<string>(type: "text", nullable: true),
                    CompanyId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StandardEntryClassCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    CompanyEntryDescription = table.Column<string>(type: "text", nullable: true),
                    DescriptiveDate = table.Column<string>(type: "text", nullable: true),
                    EffectiveEntryDate = table.Column<string>(type: "text", nullable: true),
                    CompensationDate = table.Column<string>(type: "text", nullable: true),
                    OriginUserStatusCode = table.Column<string>(type: "text", nullable: true),
                    OriginParticipantEntityCode = table.Column<string>(type: "text", nullable: true),
                    BatchNumber = table.Column<int>(type: "integer", nullable: false),
                    NachaID = table.Column<string>(type: "character varying(64)", nullable: true)
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
                    EntryDetailID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionCode = table.Column<string>(type: "text", nullable: true),
                    ReceivingParticipantEntityCode = table.Column<string>(type: "text", nullable: true),
                    CheckDigit = table.Column<string>(type: "text", nullable: true),
                    AccountNumber = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                    Amount = table.Column<decimal>(type: "money", nullable: true),
                    RecipIdNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    RecipUserName = table.Column<string>(type: "character varying(22)", maxLength: 22, nullable: true),
                    DiscreData = table.Column<string>(type: "text", nullable: true),
                    AddendumIndicator = table.Column<string>(type: "text", nullable: true),
                    SequenceNumber = table.Column<string>(type: "text", nullable: true),
                    NachaID = table.Column<string>(type: "character varying(64)", nullable: true)
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
                    FileControlID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchCount = table.Column<int>(type: "integer", nullable: false),
                    BlockCount = table.Column<int>(type: "integer", nullable: false),
                    EntryAddendaCount = table.Column<int>(type: "integer", nullable: false),
                    TotalControl = table.Column<decimal>(type: "money", nullable: false),
                    TotalDebitAmount = table.Column<decimal>(type: "money", nullable: false),
                    TotalCreditAmount = table.Column<decimal>(type: "money", nullable: false),
                    NachaID = table.Column<string>(type: "character varying(64)", nullable: true)
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
                name: "AchTransactionAddenda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AchTransactionId = table.Column<int>(type: "integer", nullable: false),
                    AddendaType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Information = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[] { 1, "Menú principal", true, "Principal" });

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
                table: "Roles",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1"), "Acceso completo al sistema", "Admin" },
                    { new Guid("a51746c2-0710-4d79-97b1-5b4368326f56"), "Operaciones básicas sobre ACH", "ACH.Operator" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "UpdatedAt", "Username" },
                values: new object[] { new Guid("0f7d6a26-df0e-4b14-8734-3280c1da6e3d"), new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "admin@achinterbank.local", "Administrador ACH", true, "3eb3fe66b31e3b4d10fa70b5cad49c7112294af6ae4e476a1c405155d45aa121", new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "admin" });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Exact", "Icon", "IsActive", "Label", "MenuId", "Order", "ParentId", "Route" },
                values: new object[,]
                {
                    { 1, true, "dashboard", true, "Dashboard", 1, 1, null, "/dashboard" },
                    { 2, false, "group", true, "Usuarios", 1, 2, null, "/users" },
                    { 4, true, "schedule", true, "Ciclos ACH", 1, 4, null, "/ach-cycles" },
                    { 5, false, "inventory", true, "Catálogos", 1, 5, null, "/catalogs" },
                    { 6, false, "swap_horiz", true, "Transacciones", 1, 6, null, "/transactions" },
                    { 9, true, "category", true, "Navegación", 1, 7, null, "/navigation/menu-items" },
                    { 10, true, "security", true, "Seguridad NACHA", 1, 8, null, "/nacha-security/certificates" },
                    { 18, false, "timer", true, "Programador", 1, 9, null, "/scheduler" },
                    { 22, true, "history", true, "Auditoría", 1, 10, null, "/audit-logs" }
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
                values: new object[] { new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1"), new Guid("0f7d6a26-df0e-4b14-8734-3280c1da6e3d"), new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

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
                    { 22, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") }
                });

            migrationBuilder.InsertData(
                table: "MenuItemRoles",
                columns: new[] { "MenuItemId", "RoleId" },
                values: new object[,]
                {
                    { 2, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 6, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 6, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") },
                    { 9, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") }
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
                    { 20, true, "view_column", true, "Layouts NACHA", 1, 2, 4, "/ach-cycles/nacha/layouts" },
                    { 21, true, "lock", true, "Sobre digital", 1, 1, 10, "/nacha-security/sobre-digital" },
                    { 23, true, "policy", true, "Reglas de contraseña", 1, 3, 2, "/users/password-rules" }
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
                    { 20, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") },
                    { 23, new Guid("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf") }
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
                    { 23, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AchBatches_AchCycleId",
                table: "AchBatches",
                column: "AchCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_AchCycles_ClearingHouseId_ProcessingDate_CycleName",
                table: "AchCycles",
                columns: new[] { "ClearingHouseId", "ProcessingDate", "CycleName" },
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
                name: "IX_CustomerAddresses_CustomerId",
                table: "CustomerAddresses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerEmails_CustomerId_IsPrimary",
                table: "CustomerEmails",
                columns: new[] { "CustomerId", "IsPrimary" },
                unique: true,
                filter: "\"IsPrimary\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPhones_CustomerId_IsPrimary",
                table: "CustomerPhones",
                columns: new[] { "CustomerId", "IsPrimary" },
                unique: true,
                filter: "\"IsPrimary\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_DocumentType_DocumentNumber",
                table: "Customers",
                columns: new[] { "DocumentType", "DocumentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntryDetails_NachaID",
                table: "EntryDetails",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_FileControls_NachaID",
                table: "FileControls",
                column: "NachaID");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInstitutions_ClearingHouseId",
                table: "FinancialInstitutions",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionClearingHousePreferences_ClearingHouseId",
                table: "InstitutionClearingHousePreferences",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionClearingHousePreferences_FinancialInstitutionId_~",
                table: "InstitutionClearingHousePreferences",
                columns: new[] { "FinancialInstitutionId", "ClearingHouseId" },
                unique: true);

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
                name: "IX_NachaHeaders_AchCycleId",
                table: "NachaHeaders",
                column: "AchCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_NachaHeaders_ClearingHouseId",
                table: "NachaHeaders",
                column: "ClearingHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_NachaRecordFields_NachaRecordLayoutId",
                table: "NachaRecordFields",
                column: "NachaRecordLayoutId");

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
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
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
                unique: true);

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
                name: "AchTransactionAddenda");

            migrationBuilder.DropTable(
                name: "AddendaRecords");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "BankHolidays");

            migrationBuilder.DropTable(
                name: "BatchControls");

            migrationBuilder.DropTable(
                name: "BatchHeaders");

            migrationBuilder.DropTable(
                name: "BrandingSettings");

            migrationBuilder.DropTable(
                name: "ClearingHouseCycleConfigs");

            migrationBuilder.DropTable(
                name: "ClearingHouseSpecialDates");

            migrationBuilder.DropTable(
                name: "CustomerAddresses");

            migrationBuilder.DropTable(
                name: "CustomerEmails");

            migrationBuilder.DropTable(
                name: "CustomerPhones");

            migrationBuilder.DropTable(
                name: "DigitalEnvelopeCertificates");

            migrationBuilder.DropTable(
                name: "EntryDetails");

            migrationBuilder.DropTable(
                name: "FileControls");

            migrationBuilder.DropTable(
                name: "InstitutionClearingHousePreferences");

            migrationBuilder.DropTable(
                name: "MenuItemPermissions");

            migrationBuilder.DropTable(
                name: "MenuItemRoles");

            migrationBuilder.DropTable(
                name: "NachaRecordFields");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "PasswordRuleSettings");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "TaskExecutionLog");

            migrationBuilder.DropTable(
                name: "TaskParameters");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "AchTransactions");

            migrationBuilder.DropTable(
                name: "NachaHeaders");

            migrationBuilder.DropTable(
                name: "MenuItems");

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
                name: "AchBatches");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "FinancialInstitutions");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropTable(
                name: "AchCycles");

            migrationBuilder.DropTable(
                name: "ClearingHouses");

            migrationBuilder.DropTable(
                name: "ClearingHouseConfigs");
        }
    }
}
