using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer;

/// <summary>Columna de configuración por cámara. Las cámaras existentes quedan desactivadas.</summary>
[DbContext(typeof(AchDbContext))]
[Migration("20261102210000_AchColombiaAutomaticReturnConfigurationI01")]
public partial class AchColombiaAutomaticReturnConfigurationI01 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AutomaticReturnMode",
            table: "ClearingHouseConfigs",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "DESACTIVADO");
        migrationBuilder.AddCheckConstraint(
            name: "CK_ClearingHouseConfigs_AutomaticReturnMode",
            table: "ClearingHouseConfigs",
            sql: "[AutomaticReturnMode] IN ('DESACTIVADO', 'CON_APROBACION', 'AUTOMATICO')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_ClearingHouseConfigs_AutomaticReturnMode",
            table: "ClearingHouseConfigs");
        migrationBuilder.DropColumn(name: "AutomaticReturnMode", table: "ClearingHouseConfigs");
    }
}
