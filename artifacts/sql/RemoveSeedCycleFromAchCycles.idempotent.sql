START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260511220904_RemoveSeedCycleFromAchCycles') THEN
    DELETE FROM "AchCycles" c
    WHERE c."Id" = 'SEED-CYCLE'
      AND NOT EXISTS (SELECT 1 FROM "AchBatches" b WHERE b."AchCycleId" = c."Id")
      AND NOT EXISTS (SELECT 1 FROM "AchTransactions" t WHERE t."AchCycleId" = c."Id")
      AND NOT EXISTS (SELECT 1 FROM "AchFileExports" f WHERE f."AchCycleId" = c."Id")
      AND NOT EXISTS (SELECT 1 FROM "NachaHeaders" h WHERE h."AchCycleId" = c."Id")
      AND NOT EXISTS (SELECT 1 FROM "CenitCycleExecutions" e WHERE e."AchCycleId" = c."Id")
      AND NOT EXISTS (SELECT 1 FROM "CenitCycleQueue" q WHERE q."TargetAchCycleId" = c."Id")
      AND NOT EXISTS (SELECT 1 FROM "AchReturnGenerated" r WHERE r."ReturnCycleId" = c."Id")
      AND NOT EXISTS (SELECT 1 FROM "ContrapartidaDispatchBatches" cb WHERE cb."AchCycleId" = c."Id")
      AND NOT EXISTS (SELECT 1 FROM "ContrapartidaDispatchItems" ci WHERE ci."AchCycleId" = c."Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260511220904_RemoveSeedCycleFromAchCycles') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260511220904_RemoveSeedCycleFromAchCycles', '10.0.5');
    END IF;
END $EF$;
COMMIT;

