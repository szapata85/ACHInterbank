using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres;

[DbContext(typeof(AchDbContext))]
[Migration("20261012233000_IntegrationsContractVersionConstraints")]
public partial class IntegrationsContractVersionConstraints : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM "IntegrationContractVersions"
                    WHERE "Status" = 3
                    GROUP BY "MethodId"
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'P1.3 constraint refused: more than one Active contract version exists for a method.';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM "IntegrationMappingSets"
                    WHERE "ContractVersionId" IS NOT NULL AND "Version" > 0
                    GROUP BY "ContractVersionId", "Version"
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'P1.3 constraint refused: duplicate mapping version exists inside one contract version.';
                END IF;
            END $$;
            """);

        migrationBuilder.DropIndex(
            name: "IX_IntegrationMethodParameters_ContractVersionId_ParameterPath",
            table: "IntegrationMethodParameters");
        migrationBuilder.DropIndex(
            name: "IX_IntegrationMethodParameters_MethodId_ParameterPath",
            table: "IntegrationMethodParameters");

        migrationBuilder.CreateIndex(
            name: "UX_IntegrationContractVersions_Method_Active",
            table: "IntegrationContractVersions",
            column: "MethodId",
            unique: true,
            filter: "\"Status\" = 3");

        migrationBuilder.CreateIndex(
            name: "UX_IntegrationMethodParameters_ContractVersion_ParameterPath",
            table: "IntegrationMethodParameters",
            columns: new[] { "ContractVersionId", "ParameterPath" },
            unique: true,
            filter: "\"ContractVersionId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "UX_IntegrationMethodParameters_Legacy_Method_ParameterPath",
            table: "IntegrationMethodParameters",
            columns: new[] { "MethodId", "ParameterPath" },
            unique: true,
            filter: "\"ContractVersionId\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "UX_IntegrationMappingSets_ContractVersion_Version",
            table: "IntegrationMappingSets",
            columns: new[] { "ContractVersionId", "Version" },
            unique: true,
            filter: "\"ContractVersionId\" IS NOT NULL AND \"Version\" > 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM "IntegrationMethodParameters"
                    GROUP BY "MethodId", "ParameterPath"
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'P1.3 downgrade refused: contract history now contains repeated parameter paths across versions.';
                END IF;
            END $$;
            """);

        migrationBuilder.DropIndex(name: "UX_IntegrationContractVersions_Method_Active", table: "IntegrationContractVersions");
        migrationBuilder.DropIndex(name: "UX_IntegrationMethodParameters_ContractVersion_ParameterPath", table: "IntegrationMethodParameters");
        migrationBuilder.DropIndex(name: "UX_IntegrationMethodParameters_Legacy_Method_ParameterPath", table: "IntegrationMethodParameters");
        migrationBuilder.DropIndex(name: "UX_IntegrationMappingSets_ContractVersion_Version", table: "IntegrationMappingSets");

        migrationBuilder.CreateIndex(
            name: "IX_IntegrationMethodParameters_ContractVersionId_ParameterPath",
            table: "IntegrationMethodParameters",
            columns: new[] { "ContractVersionId", "ParameterPath" });

        migrationBuilder.CreateIndex(
            name: "IX_IntegrationMethodParameters_MethodId_ParameterPath",
            table: "IntegrationMethodParameters",
            columns: new[] { "MethodId", "ParameterPath" },
            unique: true);
    }
}
