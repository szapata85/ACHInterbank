using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddClearingHouseRulesMenuAndRuntimeSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Exact", "Icon", "IsActive", "Label", "MenuId", "Order", "ParentId", "Route" },
                values: new object[] { 32, true, "rule", true, "Reglas por camara", 1, 6, 6, "/transactions/clearing-house-rules" });

            migrationBuilder.InsertData(
                table: "MenuItemPermissions",
                columns: new[] { "MenuItemId", "PermissionId" },
                values: new object[,]
                {
                    { 32, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") },
                    { 32, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") }
                });

            migrationBuilder.InsertData(
                table: "MenuItemRoles",
                columns: new[] { "MenuItemId", "RoleId" },
                values: new object[,]
                {
                    { 32, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") },
                    { 32, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") }
                });

            migrationBuilder.Sql("""
                WITH ach AS (
                    SELECT "Id"
                    FROM "ClearingHouses"
                    WHERE "Code" ILIKE '%ACH%' OR "Name" ILIKE '%ACH Colombia%' OR "Name" ILIKE '%ACH%'
                    ORDER BY "Id"
                    LIMIT 1
                ),
                cenit AS (
                    SELECT "Id"
                    FROM "ClearingHouses"
                    WHERE "Code" ILIKE '%CENIT%' OR "Name" ILIKE '%CENIT%'
                    ORDER BY "Id"
                    LIMIT 1
                ),
                desired AS (
                    SELECT
                        (SELECT "Id" FROM ach) AS clearing_house_id,
                        'Debit' AS transaction_nature,
                        'Debit' AS transaction_type,
                        true AS requires_prenotification,
                        'Mandatory' AS prenotification_mode,
                        true AS requires_receiver_identification_validation,
                        'Mandatory' AS receiver_identification_validation_mode,
                        'MAN-004 ACH Colombia V32' AS normative_source,
                        '2.11.4, 2.11.4.1, 2.11.4.2, 2.11.6' AS normative_reference,
                        'Debito ACH Colombia: prenotificacion tecnica obligatoria previa al proceso de debito; receptor valida cuenta e identificacion segun norma.' AS notes
                    UNION ALL
                    SELECT
                        (SELECT "Id" FROM ach),
                        'Credit',
                        'Credit',
                        false,
                        'Optional',
                        false,
                        'Optional',
                        'MAN-004 ACH Colombia V32',
                        '2.10.2, 2.10.3, 2.10.3.1, 2.10.3.2',
                        'Credito ACH Colombia: prenotificacion discrecional/opcional; no bloquea exportacion monetaria si no fue enviada.'
                    UNION ALL
                    SELECT
                        (SELECT "Id" FROM cenit),
                        'Debit',
                        'Debit',
                        true,
                        'Mandatory',
                        true,
                        'Mandatory',
                        'CENIT DSP-152 Anexo 2',
                        '4.7 Prenotificaciones',
                        'Debito CENIT: antes de una entrada debito el originador debe enviar notificacion previa/prenotificacion con addenda.'
                    UNION ALL
                    SELECT
                        (SELECT "Id" FROM cenit),
                        'Credit',
                        'Credit',
                        false,
                        'Optional',
                        false,
                        'Optional',
                        'CENIT DSP-152 Anexo 2',
                        '4.7 Prenotificaciones',
                        'Credito CENIT: la prenotificacion credito no es obligatoria segun documento fuente.'
                )
                INSERT INTO "ClearingHouseTransactionRules" (
                    "ClearingHouseId",
                    "TransactionNature",
                    "TransactionType",
                    "RequiresPrenotification",
                    "PrenotificationMode",
                    "RequiresReceiverIdentificationValidation",
                    "ReceiverIdentificationValidationMode",
                    "AppliesToNachaExport",
                    "AppliesToMonetaryTransactions",
                    "EffectiveFrom",
                    "EffectiveTo",
                    "IsActive",
                    "NormativeSource",
                    "NormativeReference",
                    "Notes",
                    "CreatedAt",
                    "UpdatedAt")
                SELECT
                    clearing_house_id,
                    transaction_nature,
                    transaction_type,
                    requires_prenotification,
                    prenotification_mode,
                    requires_receiver_identification_validation,
                    receiver_identification_validation_mode,
                    true,
                    true,
                    TIMESTAMPTZ '2025-01-01 00:00:00+00',
                    NULL,
                    true,
                    normative_source,
                    normative_reference,
                    notes,
                    timezone('utc', now()),
                    timezone('utc', now())
                FROM desired
                WHERE clearing_house_id IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "ClearingHouseTransactionRules" existing
                      WHERE existing."ClearingHouseId" = desired.clearing_house_id
                        AND existing."TransactionNature" = desired.transaction_nature
                        AND existing."TransactionType" = desired.transaction_type
                        AND existing."EffectiveFrom" = TIMESTAMPTZ '2025-01-01 00:00:00+00'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "ClearingHouseTransactionRules"
                WHERE "EffectiveFrom" = TIMESTAMPTZ '2025-01-01 00:00:00+00'
                  AND "NormativeSource" IN ('MAN-004 ACH Colombia V32', 'CENIT DSP-152 Anexo 2')
                  AND "TransactionNature" IN ('Debit', 'Credit')
                  AND "TransactionType" IN ('Debit', 'Credit');
                """);

            migrationBuilder.DeleteData(
                table: "MenuItemPermissions",
                keyColumns: new[] { "MenuItemId", "PermissionId" },
                keyValues: new object[] { 32, new Guid("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7") });

            migrationBuilder.DeleteData(
                table: "MenuItemPermissions",
                keyColumns: new[] { "MenuItemId", "PermissionId" },
                keyValues: new object[] { 32, new Guid("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a") });

            migrationBuilder.DeleteData(
                table: "MenuItemRoles",
                keyColumns: new[] { "MenuItemId", "RoleId" },
                keyValues: new object[] { 32, new Guid("1f8602da-6415-43f8-b61d-cb396f8577f1") });

            migrationBuilder.DeleteData(
                table: "MenuItemRoles",
                keyColumns: new[] { "MenuItemId", "RoleId" },
                keyValues: new object[] { 32, new Guid("a51746c2-0710-4d79-97b1-5b4368326f56") });

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 32);
        }
    }
}
