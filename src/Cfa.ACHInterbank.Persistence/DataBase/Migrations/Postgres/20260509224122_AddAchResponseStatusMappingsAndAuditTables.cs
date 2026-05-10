using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddAchResponseStatusMappingsAndAuditTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoRespuesta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IdTransaccion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CodigoCamaraCompensacion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CodigoEntidadOrigen = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CodigoEntidadDestino = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CodigoEstadoExterno = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CodigoCausalExterna = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IdEstadoInterno = table.Column<int>(type: "integer", nullable: true),
                    IdEstadoServicioExterno = table.Column<int>(type: "integer", nullable: true),
                    EstadoInternoNombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CausalNormalizada = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DescripcionCausal = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IdTransaccionServicioExterno = table.Column<int>(type: "integer", nullable: false),
                    HashIdempotencia = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EstadoProcesamiento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MotivoNoHomologacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PermiteNotificacion = table.Column<bool>(type: "boolean", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FechaRecepcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchResponses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchResponseStatusMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodigoCamaraCompensacion = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TipoRespuesta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CodigoEstadoExterno = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CodigoCausalExterna = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IdEstadoInterno = table.Column<int>(type: "integer", nullable: false),
                    IdEstadoServicioExterno = table.Column<int>(type: "integer", nullable: false),
                    EstadoInternoNombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CausalNormalizada = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DescripcionCausalNormalizada = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RequiereCausal = table.Column<bool>(type: "boolean", nullable: false),
                    PermiteNotificacion = table.Column<bool>(type: "boolean", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaInicioVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFinVigencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchResponseStatusMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchResponseNotificationAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AchResponseId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroIntento = table.Column<int>(type: "integer", nullable: false),
                    EstadoNotificacion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IdCanal = table.Column<int>(type: "integer", nullable: false),
                    NombreCanal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdTransaccion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdEstado = table.Column<int>(type: "integer", nullable: false),
                    Causal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IdTransaccionServicioExterno = table.Column<int>(type: "integer", nullable: false),
                    DescripcionCausal = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RequestPayload = table.Column<string>(type: "text", nullable: true),
                    ResponsePayload = table.Column<string>(type: "text", nullable: true),
                    ExisteError = table.Column<bool>(type: "boolean", nullable: true),
                    CodigoError = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DescripcionError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ErrorTecnico = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchResponseNotificationAttempts");

            migrationBuilder.DropTable(
                name: "AchResponseStatusMappings");

            migrationBuilder.DropTable(
                name: "AchResponses");
        }
    }
}
