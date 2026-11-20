using System.Text;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer;

/// <summary>
/// Instala/actualiza las tablas internas de Quartz.NET 4.7.1 utilizando el mismo
/// SQL no destructivo que antes ejecutaba el contenedor quartz-schema.
/// El script versionado se incorpora a la DLL para no depender del filesystem de Docker.
/// </summary>
[DbContext(typeof(AchDbContext))]
[Migration("20261118110000_QuartzSqlServer471Schema")]
public sealed class QuartzSqlServer471Schema : Migration
{
    private const string ResourceName = "ACHInterbank.Migrations.QuartzSqlServer471.sql";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(QuartzSqlServer471Schema).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Falta el SQL embebido de Quartz: {ResourceName}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        string script = reader.ReadToEnd();

        // GO es una directiva de sqlcmd/SSMS, no una instruccion de T-SQL.
        // EF Core necesita recibir cada lote por separado y dentro de su transaccion.
        foreach (string batch in SplitSqlServerBatches(script))
        {
            migrationBuilder.Sql(batch);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intencionalmente no destructivo: nunca borrar jobs, triggers ni historial
        // persistido por Quartz al revertir una migracion de la aplicacion.
        // Una reversion solamente retira esta version del historial de EF Core.
    }

    private static IEnumerable<string> SplitSqlServerBatches(string script)
    {
        using var input = new StringReader(script);
        var batch = new StringBuilder();
        string line;
        while ((line = input.ReadLine()) is not null)
        {
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                if (batch.Length != 0 && !string.IsNullOrWhiteSpace(batch.ToString()))
                {
                    yield return batch.ToString();
                }
                batch.Clear();
            }
            else
            {
                batch.AppendLine(line);
            }
        }
        if (batch.Length != 0 && !string.IsNullOrWhiteSpace(batch.ToString()))
        {
            yield return batch.ToString();
        }
    }
}
