using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres;

public partial class BackfillNachaFileNamingRules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            DECLARE
                default_source_count integer;
                default_source_id integer;
                default_routing text;
                default_transit text;
                ach_clearing_house_id integer;
                cenit_clearing_house_id integer;
                now_utc timestamptz := timezone('utc', now());
                effective_from timestamptz := '2026-01-01T00:00:00Z'::timestamptz;
            BEGIN
                SELECT COUNT(*)
                INTO default_source_count
                FROM "FinancialInstitutions"
                WHERE "IsDefaultSource" = true;

                IF default_source_count <> 1 THEN
                    RAISE EXCEPTION 'Debe existir exactamente una institucion FinancialInstitution.IsDefaultSource=true. Encontradas: %.', default_source_count;
                END IF;

                SELECT "Id", "RoutingNumber", "TransitCode"
                INTO default_source_id, default_routing, default_transit
                FROM "FinancialInstitutions"
                WHERE "IsDefaultSource" = true
                ORDER BY "Id"
                LIMIT 1;

                IF default_routing IS NULL OR btrim(default_routing) = '' THEN
                    RAISE EXCEPTION 'La institucion financiera origen % no tiene RoutingNumber configurado.', default_source_id;
                END IF;

                IF default_transit IS NULL OR btrim(default_transit) = '' THEN
                    RAISE EXCEPTION 'La institucion financiera origen % no tiene TransitCode configurado.', default_source_id;
                END IF;

                SELECT "Id"
                INTO ach_clearing_house_id
                FROM "ClearingHouses"
                WHERE "Code" IN ('ACHCOL', 'ACH')
                ORDER BY "Id"
                LIMIT 1;

                IF ach_clearing_house_id IS NULL THEN
                    RAISE EXCEPTION 'Falta la camara ACH Colombia para sembrar reglas de naming.';
                END IF;

                SELECT "Id"
                INTO cenit_clearing_house_id
                FROM "ClearingHouses"
                WHERE "Code" = 'CENIT'
                ORDER BY "Id"
                LIMIT 1;

                IF cenit_clearing_house_id IS NULL THEN
                    RAISE EXCEPTION 'Falta la camara CENIT para sembrar reglas de naming.';
                END IF;

                IF NOT EXISTS (
                    SELECT 1
                    FROM "NachaFileNamingRules"
                    WHERE "ClearingHouseId" = ach_clearing_house_id
                      AND "FileDirection" = 'Outbound'
                      AND "NamePattern" = 'RRRRTTT.ZZZ.1'
                ) THEN
                    INSERT INTO "NachaFileNamingRules"
                        ("ClearingHouseId","SourceFinancialInstitutionId","FileDirection","NamePattern","Extension",
                         "DailySequenceMin","DailySequenceMax","InternalFileIdMappingMode","RequiresNameHeaderEntityMatch",
                         "IsActive","EffectiveFrom","EffectiveTo","NormativeSource","NormativeReference","Notes","CreatedAt","UpdatedAt")
                    VALUES
                        (ach_clearing_house_id, default_source_id, 'Outbound', 'RRRRTTT.ZZZ.1', '.ach',
                         1, 36, 'Alphanumeric36', true,
                         true, effective_from, null,
                         'MAN-004 ACH Colombia V32',
                         '6.1.10.1 / 6.1.10.3',
                         'Regla outbound oficial ACH Colombia. ReturnOut reutiliza esta regla con scope separado de secuencia.',
                         now_utc, now_utc);
                END IF;

                IF NOT EXISTS (
                    SELECT 1
                    FROM "NachaFileNamingRules"
                    WHERE "ClearingHouseId" = cenit_clearing_house_id
                      AND "FileDirection" = 'Outbound'
                      AND "NamePattern" = 'RRRRTTT.ZZZ.1'
                ) THEN
                    INSERT INTO "NachaFileNamingRules"
                        ("ClearingHouseId","SourceFinancialInstitutionId","FileDirection","NamePattern","Extension",
                         "DailySequenceMin","DailySequenceMax","InternalFileIdMappingMode","RequiresNameHeaderEntityMatch",
                         "IsActive","EffectiveFrom","EffectiveTo","NormativeSource","NormativeReference","Notes","CreatedAt","UpdatedAt")
                    VALUES
                        (cenit_clearing_house_id, default_source_id, 'Outbound', 'RRRRTTT.ZZZ.1', '.ach',
                         1, 36, 'Alphanumeric36', true,
                         true, effective_from, null,
                         'CENIT-DSP-152-Anexo-2',
                         'Homologacion operativa actual',
                         'Regla outbound homologada para CENIT mientras no exista naming distinto documentado. ReturnOut reutiliza esta regla con scope separado de secuencia.',
                         now_utc, now_utc);
                END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "NachaFileNamingRules"
            WHERE "NamePattern" = 'RRRRTTT.ZZZ.1'
              AND "FileDirection" = 'Outbound'
              AND "ClearingHouseId" IN (
                  SELECT "Id" FROM "ClearingHouses" WHERE "Code" IN ('ACHCOL', 'ACH', 'CENIT')
              );
            """);
    }
}
