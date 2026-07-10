import { expect } from '@playwright/test';
import { G36Postgres, type AchCycleSnapshot } from './g36-postgres';
import {
  G36SqlServer,
  sqlNullableBoolean,
  sqlNullableNumber,
  sqlNullableString,
  sqlString
} from './g36-sqlserver';

export type { AchCycleSnapshot } from './g36-postgres';
export { pollUntil } from './g36-postgres';

export type TransactionRow = {
  id: number;
  transactionExternalId: string;
  achCycleId: string;
  clearingHouseId: number;
  sourceInstitutionId: number | null;
  destinationInstitutionId: number;
  type: number;
};

export type DispatchEvidenceRow = {
  result: number;
  externalResponseCode: string | null;
  externalResponseMessage: string | null;
  errorCode: string | null;
  errorMessage: string | null;
  requestPayloadXml: string;
  responsePayloadXml: string;
  soapMethodName: string | null;
  soapEndpoint: string | null;
  executionMode: string | null;
  durationMs: number | string | null;
  soapResponseCode: string | null;
  soapResponseDescription: string | null;
  soapTechnicalStatus: string | null;
  isSuccessful: boolean | number | null;
  isFunctionalRejection: boolean | number | null;
  isTechnicalFailure: boolean | number | null;
  technicalException: string | null;
  requestedBy: string;
  batchStatus: number;
  correlationId: string;
};

export type MappingRuleSnapshot = {
  id: number | string;
  sourceKind: number;
  sourceCatalogFieldId: number | null;
  sourceFieldPath: string;
  fixedValue: string | null;
  defaultValue: string | null;
  transformationCode: string | null;
  formatMask: string | null;
  priority: number;
  requiredOverride: boolean | number | null;
  enabled: boolean | number;
};

type RuntimeProvider = 'SqlServer' | 'Postgres';

export class G36RuntimeDb {
  private readonly provider: RuntimeProvider;
  private readonly dispatchTriggeredBy: string;
  private readonly postgres: G36Postgres | null;
  private readonly sqlServer: G36SqlServer | null;

  constructor(dispatchTriggeredBy: string, provider = readProvider()) {
    this.provider = normalizeProvider(provider);
    this.dispatchTriggeredBy = dispatchTriggeredBy;
    this.postgres = this.provider === 'Postgres'
      ? new G36Postgres({ requireExplicitConfig: true })
      : null;
    this.sqlServer = this.provider === 'SqlServer'
      ? new G36SqlServer()
      : null;
  }

  get providerName(): RuntimeProvider {
    return this.provider;
  }

  async assertReady(): Promise<void> {
    if (this.postgres) {
      await this.postgres.assertReady();
      return;
    }

    this.sqlServer!.assertReady();
    const rows = this.sqlQuery<{ table_name: string }>(
      `SELECT [name] AS [table_name]
       FROM sys.tables
       WHERE [name] IN ('AchCycles', 'AchTransactions', 'ContrapartidaDispatchBatches', 'ContrapartidaDispatchItems', 'ContrapartidaDispatchAttempts')`
    );
    expect(rows.map((row) => row.table_name).sort(), 'La base runtime SQL Server debe estar provisionada para Proc_Contrapartidas.')
      .toEqual(['AchCycles', 'AchTransactions', 'ContrapartidaDispatchAttempts', 'ContrapartidaDispatchBatches', 'ContrapartidaDispatchItems'].sort());
  }

  async close(): Promise<void> {
    await this.postgres?.close();
    this.sqlServer?.close();
  }

  async configureProcContrapartidasExpectedMapping(): Promise<MappingRuleSnapshot[]> {
    if (this.postgres) {
      return this.configureProcContrapartidasExpectedMappingPostgres();
    }

    const targetPaths = procContrapartidasTargetPaths();
    const pathList = targetPaths.map(sqlString).join(', ');
    const snapshot = this.sqlQuery<MappingRuleSnapshot>(
      `WITH Published AS (
         SELECT TOP (1) s.[Id] AS [MappingSetId], s.[MethodId]
         FROM [IntegrationMappingSets] s
         JOIN [IntegrationMethods] m ON m.[Id] = s.[MethodId]
         WHERE m.[Code] = N'WSCFAACH.Proc_Contrapartidas'
           AND s.[Status] = 2
           AND s.[IsActive] = 1
         ORDER BY s.[Version] DESC
       )
       SELECT r.[Id] AS [id],
              r.[SourceKind] AS [sourceKind],
              r.[SourceCatalogFieldId] AS [sourceCatalogFieldId],
              r.[SourceFieldPath] AS [sourceFieldPath],
              r.[FixedValue] AS [fixedValue],
              r.[DefaultValue] AS [defaultValue],
              r.[TransformationCode] AS [transformationCode],
              r.[FormatMask] AS [formatMask],
              r.[Priority] AS [priority],
              r.[RequiredOverride] AS [requiredOverride],
              r.[Enabled] AS [enabled]
       FROM [IntegrationMappingRules] r
       JOIN [IntegrationMethodParameters] p ON p.[Id] = r.[ParameterId]
       JOIN Published pub ON pub.[MappingSetId] = r.[MappingSetId]
       WHERE p.[ParameterPath] IN (${pathList})`
    );

    expect(snapshot, 'El mapping publicado de Proc_Contrapartidas debe contener las reglas funcionales requeridas para esta prueba.')
      .toHaveLength(targetPaths.length);

    this.updateProcContrapartidasRuleSqlServer('OFCTA', 1, 'transaction.sourceaccountnumber', null, null);
    this.updateProcContrapartidasRuleSqlServer('OFDD', 6, 'constant.value', 'TRANSFER  ', 'TRANSFER  ');
    this.updateProcContrapartidasRuleSqlServer('OFMONDEB', 1, 'transaction.amount', null, '0');
    this.updateProcContrapartidasRuleSqlServer('OFMONCRE', 6, 'constant.value', '0', '0');
    this.updateProcContrapartidasRuleSqlServer('OFST', 6, 'constant.value', 'OO', 'OO');
    this.updateProcContrapartidasRuleSqlServer('OFIDTX', 6, 'constant.value', '0', '0');
    this.updateProcContrapartidasRuleSqlServer('OFIDREVER', 6, 'constant.value', '0', '0');
    this.updateProcContrapartidasRuleSqlServer('OFIDEBAPLI', 6, 'constant.value', '1', '1');
    this.updateProcContrapartidasRuleSqlServer('OFLIBRE', 1, 'transaction.reference', null, null);
    this.updateProcContrapartidasRuleSqlServer('OFLIBRE1', 1, 'transaction.id', null, null);

    return snapshot;
  }

  async restoreProcContrapartidasMapping(snapshot: MappingRuleSnapshot[]): Promise<void> {
    if (snapshot.length === 0) {
      return;
    }

    if (this.postgres) {
      for (const row of snapshot) {
        await this.postgres.execute(
          `UPDATE "IntegrationMappingRules"
           SET "SourceKind" = $2,
               "SourceCatalogFieldId" = $3,
               "SourceFieldPath" = $4,
               "FixedValue" = $5,
               "DefaultValue" = $6,
               "TransformationCode" = $7,
               "FormatMask" = $8,
               "Priority" = $9,
               "RequiredOverride" = $10,
               "Enabled" = $11
           WHERE "Id" = $1`,
          [
            row.id,
            row.sourceKind,
            row.sourceCatalogFieldId,
            row.sourceFieldPath,
            row.fixedValue,
            row.defaultValue,
            row.transformationCode,
            row.formatMask,
            row.priority,
            row.requiredOverride,
            row.enabled
          ]
        );
      }
      return;
    }

    for (const row of snapshot) {
      this.sqlExecute(
        `UPDATE [IntegrationMappingRules]
         SET [SourceKind] = ${row.sourceKind},
             [SourceCatalogFieldId] = ${sqlNullableNumber(row.sourceCatalogFieldId)},
             [SourceFieldPath] = ${sqlNullableString(row.sourceFieldPath)},
             [FixedValue] = ${sqlNullableString(row.fixedValue)},
             [DefaultValue] = ${sqlNullableString(row.defaultValue)},
             [TransformationCode] = ${sqlNullableString(row.transformationCode)},
             [FormatMask] = ${sqlNullableString(row.formatMask)},
             [Priority] = ${row.priority},
             [RequiredOverride] = ${sqlNullableBoolean(toNullableBoolean(row.requiredOverride))},
             [Enabled] = ${row.enabled === true || row.enabled === 1 ? 1 : 0}
         WHERE [Id] = ${sqlString(String(row.id))}`
      );
    }
  }

  async findTransactionByExternalId(transactionExternalId: string): Promise<TransactionRow | null> {
    if (this.postgres) {
      const rows = await this.postgres.query<TransactionRow>(
        `SELECT t."Id" AS id,
                t."TransactionExternalId" AS "transactionExternalId",
                t."AchCycleId" AS "achCycleId",
                c."ClearingHouseId" AS "clearingHouseId",
                t."SourceInstitutionId" AS "sourceInstitutionId",
                t."DestinationInstitutionId" AS "destinationInstitutionId",
                t."Type" AS type
         FROM "AchTransactions" t
         JOIN "AchCycles" c ON c."Id" = t."AchCycleId"
         WHERE t."TransactionExternalId" = $1
         ORDER BY t."Id" DESC
         LIMIT 1`,
        [transactionExternalId]
      );
      return rows[0] ?? null;
    }

    const rows = this.sqlQuery<TransactionRow>(
      `SELECT TOP (1)
              t.[Id] AS [id],
              t.[TransactionExternalId] AS [transactionExternalId],
              t.[AchCycleId] AS [achCycleId],
              c.[ClearingHouseId] AS [clearingHouseId],
              t.[SourceInstitutionId] AS [sourceInstitutionId],
              t.[DestinationInstitutionId] AS [destinationInstitutionId],
              t.[Type] AS [type]
       FROM [AchTransactions] t
       JOIN [AchCycles] c ON c.[Id] = t.[AchCycleId]
       WHERE t.[TransactionExternalId] = ${sqlString(transactionExternalId)}
       ORDER BY t.[Id] DESC`
    );

    return rows[0] ?? null;
  }

  async loadCycleSnapshot(cycleId: string, clearingHouseId: number): Promise<AchCycleSnapshot> {
    if (this.postgres) {
      const rows = await this.postgres.query<AchCycleSnapshot>(
        `SELECT "Id" AS id,
                "CycleName" AS "cycleName",
                "ProcessingDate" AS "processingDate",
                "CutoffTime"::text AS "cutoffTime",
                "StartTime"::text AS "startTime",
                "EndTime"::text AS "endTime",
                "RescheduleOnHoliday" AS "rescheduleOnHoliday",
                "ClearingHouseId" AS "clearingHouseId",
                "UpdatedAt" AS "updatedAt"
         FROM "AchCycles"
         WHERE "Id" = $1 AND "ClearingHouseId" = $2`,
        [cycleId, clearingHouseId]
      );
      expect(rows, `Debe existir el ciclo ${cycleId}.`).toHaveLength(1);
      return rows[0];
    }

    const rows = this.sqlQuery<AchCycleSnapshot>(
      `SELECT [Id] AS [id],
              [CycleName] AS [cycleName],
              CONVERT(varchar(10), [ProcessingDate], 23) AS [processingDate],
              CONVERT(varchar(16), [CutoffTime], 114) AS [cutoffTime],
              CONVERT(varchar(16), [StartTime], 114) AS [startTime],
              CONVERT(varchar(16), [EndTime], 114) AS [endTime],
              [RescheduleOnHoliday] AS [rescheduleOnHoliday],
              [ClearingHouseId] AS [clearingHouseId],
              CONVERT(varchar(33), [UpdatedAt], 126) AS [updatedAt]
       FROM [AchCycles]
       WHERE [Id] = ${sqlString(cycleId)} AND [ClearingHouseId] = ${clearingHouseId}`
    );

    expect(rows, `Debe existir el ciclo ${cycleId}.`).toHaveLength(1);
    return rows[0];
  }

  async loadCycleSnapshots(): Promise<AchCycleSnapshot[]> {
    if (this.postgres) {
      const rows = await this.postgres.query<AchCycleSnapshot>(
        `SELECT "Id" AS id,
                "CycleName" AS "cycleName",
                "ProcessingDate" AS "processingDate",
                "CutoffTime"::text AS "cutoffTime",
                "StartTime"::text AS "startTime",
                "EndTime"::text AS "endTime",
                "RescheduleOnHoliday" AS "rescheduleOnHoliday",
                "ClearingHouseId" AS "clearingHouseId",
                "UpdatedAt" AS "updatedAt"
         FROM "AchCycles"
         ORDER BY "ClearingHouseId", "Id"`
      );
      expect(rows.length, 'Debe existir al menos un ciclo ACH runtime para la prueba.').toBeGreaterThan(0);
      return rows;
    }

    const rows = this.sqlQuery<AchCycleSnapshot>(
      `SELECT [Id] AS [id],
              [CycleName] AS [cycleName],
              CONVERT(varchar(10), [ProcessingDate], 23) AS [processingDate],
              CONVERT(varchar(16), [CutoffTime], 114) AS [cutoffTime],
              CONVERT(varchar(16), [StartTime], 114) AS [startTime],
              CONVERT(varchar(16), [EndTime], 114) AS [endTime],
              [RescheduleOnHoliday] AS [rescheduleOnHoliday],
              [ClearingHouseId] AS [clearingHouseId],
              CONVERT(varchar(33), [UpdatedAt], 126) AS [updatedAt]
       FROM [AchCycles]
       ORDER BY [ClearingHouseId], [Id]`
    );
    expect(rows.length, 'Debe existir al menos un ciclo ACH runtime para la prueba.').toBeGreaterThan(0);
    return rows;
  }

  async configureCycles(snapshots: readonly AchCycleSnapshot[], processingDate: string): Promise<void> {
    for (const [index, snapshot] of snapshots.entries()) {
      await this.configureCycle(snapshot, buildTemporaryCycleName(snapshot, index), processingDate);
    }
  }

  async configureCycle(snapshot: AchCycleSnapshot, cycleName: string, processingDate: string): Promise<void> {
    if (this.postgres) {
      await this.postgres.configureCycle(snapshot, cycleName, processingDate);
      return;
    }

    this.sqlExecute(
      `UPDATE [AchCycles]
       SET [CycleName] = ${sqlString(cycleName)},
           [ProcessingDate] = CONVERT(date, ${sqlString(processingDate)}, 23),
           [StartTime] = CONVERT(time, '00:00:00'),
           [EndTime] = CONVERT(time, '23:59:59'),
           [CutoffTime] = CONVERT(time, '23:59:59'),
           [RescheduleOnHoliday] = 0,
           [UpdatedAt] = SYSUTCDATETIME()
       WHERE [Id] = ${sqlString(snapshot.id)}`
    );
  }

  async restoreCycle(snapshot: AchCycleSnapshot): Promise<void> {
    if (this.postgres) {
      await this.postgres.restoreCycle(snapshot);
      return;
    }

    this.sqlExecute(
      `UPDATE [AchCycles]
       SET [CycleName] = ${sqlString(snapshot.cycleName)},
           [ProcessingDate] = CONVERT(date, ${sqlString(toSqlDate(snapshot.processingDate))}, 23),
           [CutoffTime] = CONVERT(time, ${sqlString(String(snapshot.cutoffTime))}),
           [StartTime] = CONVERT(time, ${sqlString(String(snapshot.startTime))}),
           [EndTime] = CONVERT(time, ${sqlString(String(snapshot.endTime))}),
           [RescheduleOnHoliday] = ${snapshot.rescheduleOnHoliday ? 1 : 0},
           [UpdatedAt] = SYSUTCDATETIME()
      WHERE [Id] = ${sqlString(snapshot.id)}`
    );
  }

  async restoreCycles(snapshots: readonly AchCycleSnapshot[]): Promise<void> {
    for (const snapshot of snapshots) {
      await this.restoreCycle(snapshot);
    }
  }

  async countDispatchItems(transactionId: number): Promise<number> {
    if (this.postgres) {
      return Number(await this.postgres.scalar<string>(
        `SELECT COUNT(*)::text
         FROM "ContrapartidaDispatchItems"
         WHERE "AchTransactionId" = $1`,
        [transactionId]
      ) ?? 0);
    }

    return Number(this.sqlServer!.scalar<string>(
      `SELECT CONVERT(varchar(30), COUNT(*)) AS [value]
       FROM [ContrapartidaDispatchItems]
       WHERE [AchTransactionId] = ${transactionId}`
    ) ?? 0);
  }

  async findDispatchEvidence(transactionExternalId: string): Promise<DispatchEvidenceRow | null> {
    if (this.postgres) {
      const rows = await this.postgres.query<DispatchEvidenceRow>(
        `SELECT a."Result" AS result,
                a."ExternalResponseCode" AS "externalResponseCode",
                a."ExternalResponseMessage" AS "externalResponseMessage",
                a."ErrorCode" AS "errorCode",
                a."ErrorMessage" AS "errorMessage",
                a."RequestPayloadXml" AS "requestPayloadXml",
                a."ResponsePayloadXml" AS "responsePayloadXml",
                a."SoapMethodName" AS "soapMethodName",
                a."SoapEndpoint" AS "soapEndpoint",
                a."ExecutionMode" AS "executionMode",
                a."DurationMs" AS "durationMs",
                a."SoapResponseCode" AS "soapResponseCode",
                a."SoapResponseDescription" AS "soapResponseDescription",
                a."SoapTechnicalStatus" AS "soapTechnicalStatus",
                a."IsSuccessful" AS "isSuccessful",
                a."IsFunctionalRejection" AS "isFunctionalRejection",
                a."IsTechnicalFailure" AS "isTechnicalFailure",
                a."TechnicalException" AS "technicalException",
                b."RequestedBy" AS "requestedBy",
                b."Status" AS "batchStatus",
                a."CorrelationId" AS "correlationId"
         FROM "ContrapartidaDispatchAttempts" a
         JOIN "ContrapartidaDispatchItems" i ON i."Id" = a."DispatchItemId"
         JOIN "ContrapartidaDispatchBatches" b ON b."Id" = a."DispatchBatchId"
         JOIN "AchTransactions" t ON t."Id" = i."AchTransactionId"
         WHERE t."TransactionExternalId" = $1
           AND b."RequestedBy" = $2
         ORDER BY a."FinishedAtUtc" DESC
         LIMIT 1`,
        [transactionExternalId, this.dispatchTriggeredBy]
      );
      return rows[0] ?? null;
    }

    const rows = this.sqlQuery<DispatchEvidenceRow>(
      `SELECT TOP (1)
              a.[Result] AS [result],
              a.[ExternalResponseCode] AS [externalResponseCode],
              a.[ExternalResponseMessage] AS [externalResponseMessage],
              a.[ErrorCode] AS [errorCode],
              a.[ErrorMessage] AS [errorMessage],
              a.[RequestPayloadXml] AS [requestPayloadXml],
              a.[ResponsePayloadXml] AS [responsePayloadXml],
              a.[SoapMethodName] AS [soapMethodName],
              a.[SoapEndpoint] AS [soapEndpoint],
              a.[ExecutionMode] AS [executionMode],
              a.[DurationMs] AS [durationMs],
              a.[SoapResponseCode] AS [soapResponseCode],
              a.[SoapResponseDescription] AS [soapResponseDescription],
              a.[SoapTechnicalStatus] AS [soapTechnicalStatus],
              a.[IsSuccessful] AS [isSuccessful],
              a.[IsFunctionalRejection] AS [isFunctionalRejection],
              a.[IsTechnicalFailure] AS [isTechnicalFailure],
              a.[TechnicalException] AS [technicalException],
              b.[RequestedBy] AS [requestedBy],
              b.[Status] AS [batchStatus],
              a.[CorrelationId] AS [correlationId]
       FROM [ContrapartidaDispatchAttempts] a
       JOIN [ContrapartidaDispatchItems] i ON i.[Id] = a.[DispatchItemId]
       JOIN [ContrapartidaDispatchBatches] b ON b.[Id] = a.[DispatchBatchId]
       JOIN [AchTransactions] t ON t.[Id] = i.[AchTransactionId]
       WHERE t.[TransactionExternalId] = ${sqlString(transactionExternalId)}
         AND b.[RequestedBy] = ${sqlString(this.dispatchTriggeredBy)}
       ORDER BY a.[FinishedAtUtc] DESC`
    );

    return rows[0] ?? null;
  }

  private async configureProcContrapartidasExpectedMappingPostgres(): Promise<MappingRuleSnapshot[]> {
    const targetPaths = procContrapartidasTargetPaths();
    const snapshot = await this.postgres!.query<MappingRuleSnapshot>(
      `WITH "Published" AS (
         SELECT s."Id" AS "MappingSetId", s."MethodId"
         FROM "IntegrationMappingSets" s
         JOIN "IntegrationMethods" m ON m."Id" = s."MethodId"
         WHERE m."Code" = 'WSCFAACH.Proc_Contrapartidas'
           AND s."Status" = 2
           AND s."IsActive" = TRUE
         ORDER BY s."Version" DESC
         LIMIT 1
       )
       SELECT r."Id" AS id,
              r."SourceKind" AS "sourceKind",
              r."SourceCatalogFieldId" AS "sourceCatalogFieldId",
              r."SourceFieldPath" AS "sourceFieldPath",
              r."FixedValue" AS "fixedValue",
              r."DefaultValue" AS "defaultValue",
              r."TransformationCode" AS "transformationCode",
              r."FormatMask" AS "formatMask",
              r."Priority" AS priority,
              r."RequiredOverride" AS "requiredOverride",
              r."Enabled" AS enabled
       FROM "IntegrationMappingRules" r
       JOIN "IntegrationMethodParameters" p ON p."Id" = r."ParameterId"
       JOIN "Published" pub ON pub."MappingSetId" = r."MappingSetId"
       WHERE p."ParameterPath" = ANY($1::text[])`,
      [targetPaths]
    );

    expect(snapshot, 'El mapping publicado de Proc_Contrapartidas debe contener las reglas funcionales requeridas para esta prueba.')
      .toHaveLength(targetPaths.length);

    await this.updateProcContrapartidasRulePostgres('OFCTA', 1, 'transaction.sourceaccountnumber', null, null);
    await this.updateProcContrapartidasRulePostgres('OFDD', 6, 'constant.value', 'TRANSFER  ', 'TRANSFER  ');
    await this.updateProcContrapartidasRulePostgres('OFMONDEB', 1, 'transaction.amount', null, '0');
    await this.updateProcContrapartidasRulePostgres('OFMONCRE', 6, 'constant.value', '0', '0');
    await this.updateProcContrapartidasRulePostgres('OFST', 6, 'constant.value', 'OO', 'OO');
    await this.updateProcContrapartidasRulePostgres('OFIDTX', 6, 'constant.value', '0', '0');
    await this.updateProcContrapartidasRulePostgres('OFIDREVER', 6, 'constant.value', '0', '0');
    await this.updateProcContrapartidasRulePostgres('OFIDEBAPLI', 6, 'constant.value', '1', '1');
    await this.updateProcContrapartidasRulePostgres('OFLIBRE', 1, 'transaction.reference', null, null);
    await this.updateProcContrapartidasRulePostgres('OFLIBRE1', 1, 'transaction.id', null, null);

    return snapshot;
  }

  private async updateProcContrapartidasRulePostgres(
    parameterPath: string,
    sourceKind: number,
    sourceFieldPath: string,
    fixedValue: string | null,
    defaultValue: string | null
  ): Promise<void> {
    await this.postgres!.execute(
      `WITH "Published" AS (
         SELECT s."Id" AS "MappingSetId"
         FROM "IntegrationMappingSets" s
         JOIN "IntegrationMethods" m ON m."Id" = s."MethodId"
         WHERE m."Code" = 'WSCFAACH.Proc_Contrapartidas'
           AND s."Status" = 2
           AND s."IsActive" = TRUE
         ORDER BY s."Version" DESC
         LIMIT 1
       )
       UPDATE "IntegrationMappingRules" r
       SET "SourceKind" = $2,
           "SourceCatalogFieldId" = NULL,
           "SourceFieldPath" = $3,
           "FixedValue" = $4,
           "DefaultValue" = $5,
           "TransformationCode" = NULL,
           "FormatMask" = NULL,
           "RequiredOverride" = TRUE,
           "Enabled" = TRUE
       FROM "IntegrationMethodParameters" p, "Published" pub
       WHERE p."Id" = r."ParameterId"
         AND pub."MappingSetId" = r."MappingSetId"
         AND p."ParameterPath" = $1`,
      [parameterPath, sourceKind, sourceFieldPath, fixedValue, defaultValue]
    );
  }

  private updateProcContrapartidasRuleSqlServer(
    parameterPath: string,
    sourceKind: number,
    sourceFieldPath: string,
    fixedValue: string | null,
    defaultValue: string | null
  ): void {
    this.sqlExecute(
      `WITH Published AS (
         SELECT TOP (1) s.[Id] AS [MappingSetId]
         FROM [IntegrationMappingSets] s
         JOIN [IntegrationMethods] m ON m.[Id] = s.[MethodId]
         WHERE m.[Code] = N'WSCFAACH.Proc_Contrapartidas'
           AND s.[Status] = 2
           AND s.[IsActive] = 1
         ORDER BY s.[Version] DESC
       )
       UPDATE r
       SET r.[SourceKind] = ${sourceKind},
           r.[SourceCatalogFieldId] = NULL,
           r.[SourceFieldPath] = ${sqlString(sourceFieldPath)},
           r.[FixedValue] = ${sqlNullableString(fixedValue)},
           r.[DefaultValue] = ${sqlNullableString(defaultValue)},
           r.[TransformationCode] = NULL,
           r.[FormatMask] = NULL,
           r.[RequiredOverride] = 1,
           r.[Enabled] = 1
       FROM [IntegrationMappingRules] r
       JOIN [IntegrationMethodParameters] p ON p.[Id] = r.[ParameterId]
       JOIN Published pub ON pub.[MappingSetId] = r.[MappingSetId]
       WHERE p.[ParameterPath] = ${sqlString(parameterPath)}`
    );
  }

  private sqlQuery<T>(selectSql: string): T[] {
    return this.sqlServer!.query<T>(selectSql);
  }

  private sqlExecute(sql: string): void {
    this.sqlServer!.execute(sql);
  }
}

function readProvider(): string {
  return process.env['ACH_E2E_DB_PROVIDER'] ?? process.env['Database__Provider'] ?? 'Postgres';
}

function normalizeProvider(provider: string): RuntimeProvider {
  const normalized = provider.trim().toLowerCase();
  if (normalized === 'sqlserver' || normalized === 'mssql') {
    return 'SqlServer';
  }

  if (normalized === 'postgres' || normalized === 'postgresql') {
    return 'Postgres';
  }

  throw new Error(`ACH_E2E_DB_PROVIDER invalido: ${provider}. Use SqlServer o Postgres.`);
}

function procContrapartidasTargetPaths(): string[] {
  return ['OFCTA', 'OFDD', 'OFMONDEB', 'OFMONCRE', 'OFST', 'OFIDTX', 'OFIDREVER', 'OFIDEBAPLI', 'OFLIBRE', 'OFLIBRE1'];
}

function toSqlDate(value: Date | string): string {
  if (value instanceof Date) {
    return value.toISOString().slice(0, 10);
  }

  return String(value).slice(0, 10);
}

function toNullableBoolean(value: boolean | number | null): boolean | null {
  if (value === null) {
    return null;
  }

  return value === true || value === 1;
}

function buildTemporaryCycleName(snapshot: AchCycleSnapshot, index: number): string {
  const suffix = ` PW${index + 1}`;
  return `${snapshot.cycleName}`.slice(0, Math.max(1, 80 - suffix.length)) + suffix;
}
