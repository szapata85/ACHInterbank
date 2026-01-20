using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class addreturnreasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReturnReasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnReasons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnReasons_Code",
                table: "ReturnReasons",
                column: "Code",
                unique: true);

            migrationBuilder.InsertData(
                table: "ReturnReasons",
                columns: new[] { "Id", "Category", "Code", "Description" },
                values: new object[,]
                {
                    { 1, "R", "R01", "Fondos Insuficientes." },
                    { 2, "R", "R02", "Cuenta o Depósito Electrónico Cerrado (Saldado o Cancelado)." },
                    { 3, "R", "R03", "Cuenta o Depósito Electrónico No Abierto." },
                    { 4, "R", "R04", "Número Cuenta o Depósito Electrónico Inválido." },
                    { 5, "R", "R06", "Devolución Solicitada por el participante originador (por error o listas restrictivas)." },
                    { 6, "R", "R07", "Autorización de Recaudo Revocada por el usuario Receptor." },
                    { 7, "R", "R08", "Orden de No Pago (instrucción específica del usuario)." },
                    { 8, "R", "R09", "Fondos no Disponibles (saldo total suficiente, pero no el disponible)." },
                    { 9, "R", "R10", "No existe prenotificación (para débitos)." },
                    { 10, "R", "R12", "Usuario Originador no autorizado o Sucursal Vendida." },
                    { 11, "R", "R13", "Devolución por solicitud del usuario Receptor (Persona Natural: fecha/monto errado o duplicidad)." },
                    { 12, "R", "R14", "Muerte del delegado o Representante." },
                    { 13, "R", "R15", "Muerte del Beneficiario o Titular de la Cuenta." },
                    { 14, "R", "R16", "Cuenta o Depósito Electrónico Inactivo / Bloqueado." },
                    { 15, "R", "R17", "La Identificación no coincide con Cuenta." },
                    { 16, "R", "R20", "Cuenta No Habilitada (listas restrictivas OFAC/ONU o fines políticos)." },
                    { 17, "R", "R23", "Devolución crédito por solicitud del usuario (sobrepago, litigio, etc.)." },
                    { 18, "R", "R29", "Devolución débito por solicitud del usuario (Persona Jurídica)." },
                    { 19, "R", "R30", "Cliente Receptor no habilitado para depósitos electrónicos." },
                    { 20, "R", "R31", "Prenotificación no procesada por falta de información en adenda." },
                    { 21, "R", "R32", "Transacción crédito no procesada por falta de información en adenda." },
                    { 22, "R", "R33", "Excede los límites establecidos para Depósitos Electrónicos." },
                    { 23, "R", "R35", "Tipo de Cuenta Errada." },
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnReasons");
        }
    }
}
