START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260511230542_RemoveSeedMappingTransactionFromAchTransactions') THEN
    DELETE FROM "AchTransactionAddenda" a
    USING "AchTransactions" t
    WHERE a."AchTransactionId" = t."Id"
      AND t."Reference" = 'SEED-MAPPING-001'
      AND t."CompanyName" = 'SEED COMPANY'
      AND t."CompanyIdentification" = '900123456'
      AND t."TraceNumber" = '000010070000123'
      AND t."Amount" = 2500;

    DELETE FROM "AchTransactions" t
    WHERE t."Reference" = 'SEED-MAPPING-001'
      AND t."CompanyName" = 'SEED COMPANY'
      AND t."CompanyIdentification" = '900123456'
      AND t."TraceNumber" = '000010070000123'
      AND t."Amount" = 2500
      AND NOT EXISTS (SELECT 1 FROM "CenitCycleQueue" cq WHERE cq."AchTransactionId" = t."Id")
      AND NOT EXISTS (SELECT 1 FROM "LiquidityOptimizationDecisions" ld WHERE ld."AchTransactionId" = t."Id")
      AND NOT EXISTS (SELECT 1 FROM "CenitNettingDetails" nd WHERE nd."AchTransactionId" = t."Id")
      AND NOT EXISTS (SELECT 1 FROM "AchTransactionStateEvents" se WHERE se."AchTransactionId" = t."Id")
      AND NOT EXISTS (SELECT 1 FROM "IncomingNachaTransactionLinks" il WHERE il."AchTransactionId" = t."Id")
      AND NOT EXISTS (SELECT 1 FROM "ContrapartidaDispatchItems" di WHERE di."AchTransactionId" = t."Id")
      AND NOT EXISTS (SELECT 1 FROM "IncomingNachaDispatchQueue" dq WHERE dq."AchTransactionId" = t."Id");

    DELETE FROM "AchBatches" b
    WHERE b."CompanyName" = 'SEED COMPANY'
      AND b."CompanyIdentification" = '900123456'
      AND NOT EXISTS (SELECT 1 FROM "AchTransactions" t WHERE t."AchBatchId" = b."Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260511230542_RemoveSeedMappingTransactionFromAchTransactions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260511230542_RemoveSeedMappingTransactionFromAchTransactions', '10.0.5');
    END IF;
END $EF$;
COMMIT;

