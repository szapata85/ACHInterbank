using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres;

/// <summary>
/// I01-CENIT: completa la infraestructura compartida que I01-ACH ya añadió en SQL Server.
/// Aplica el valor seguro a TODAS las cámaras, sin cambiar rutas ni habilitar procesos.
/// </summary>
[DbContext(typeof(AchDbContext))]
[Migration("20261102233000_CenitAutomaticReturnConfigurationI01")]
public partial class CenitAutomaticReturnConfigurationI01 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AutomaticReturnMode", table: "ClearingHouseConfigs",
            type: "character varying(20)", maxLength: 20,
            nullable: false, defaultValue: "DESACTIVADO");
        migrationBuilder.AddCheckConstraint(
            name: "CK_ClearingHouseConfigs_AutomaticReturnMode",
            table: "ClearingHouseConfigs",
            sql: "\"AutomaticReturnMode\" IN ('DESACTIVADO', 'CON_APROBACION', 'AUTOMATICO')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reversión destructiva: usar solo tras evaluar pérdida de preferencias por cámara.
        migrationBuilder.DropCheckConstraint(
            name: "CK_ClearingHouseConfigs_AutomaticReturnMode", table: "ClearingHouseConfigs");
        migrationBuilder.DropColumn(name: "AutomaticReturnMode", table: "ClearingHouseConfigs");
    }
}
