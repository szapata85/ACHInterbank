import { expect } from '@playwright/test';
import { G36Postgres, type AchCycleSnapshot, type SqlCommand } from './g36-postgres';
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

export type IncomingNachaIngestionRow = {
  id: string;
  fileName: string;
  correlationId: string;
  ingestionStatus: string | number;
  cycleResolutionStatus: string | number;
  parsingStatus: string | number;
  resolvedAchCycleId: string | null;
  resolvedClearingHouseId: number | null;
  operationalDate: Date | string | null;
  uploadedAtUtc: Date | string;
};

export type IncomingNachaDispatchQueueRow = {
  id: string;
  incomingNachaFileIngestionId: string;
  achCycleId: string;
  queueStatus: string | number;
  attemptCount: number;
  lastErrorCode: string;
  lastErrorMessage: string;
  lastResponseCode: string;
  idempotencyDispatchKey: string;
  correlationId: string;
  functionalClass: string;
  eligibilityStatus: string;
  createdAt: Date | string;
  updatedAt: Date | string;
};

export type IncomingNachaIntegrationEvidenceRow = {
  id: string;
  dispatchQueueId: string;
  integrationType: string;
  soapMethodName: string;
  soapEndpoint: string;
  executionMode: string;
  requestPayloadXml: string;
  responsePayloadXml: string;
  soapResponseCode: string;
  soapResponseDescription: string;
  soapTechnicalStatus: string;
  isSuccessful: boolean | number;
  isFunctionalRejection: boolean | number;
  isTechnicalFailure: boolean | number;
  technicalException: string;
  correlationId: string;
  startedAtUtc: Date | string;
  finishedAtUtc: Date | string | null;
  durationMs: number | string;
};

export type IncomingPostProcessingTaskSnapshot = {
  id: number;
  status: number;
  calendarPolicy: number;
  periodicityType: number;
  n: number | null;
  minute: number | null;
  timeOfDayTicks: string | null;
  weeklyDay: number | null;
  monthDay: number | null;
  cronExpression: string | null;
  startAt: Date | string | null;
  endAt: Date | string | null;
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
       WHERE [name] IN ('AchCycles', 'AchTransactions', 'ContrapartidaDispatchBatches', 'ContrapartidaDispatchItems', 'ContrapartidaDispatchAttempts', 'IncomingNachaFileIngestions', 'IncomingNachaDispatchQueue', 'IncomingNachaIntegrationExecution')`
    );
    expect(rows.map((row) => row.table_name).sort(), 'La base runtime SQL Server debe estar provisionada para Proc_Contrapartidas.')
      .toEqual(['AchCycles', 'AchTransactions', 'ContrapartidaDispatchAttempts', 'ContrapartidaDispatchBatches', 'ContrapartidaDispatchItems', 'IncomingNachaFileIngestions', 'IncomingNachaDispatchQueue', 'IncomingNachaIntegrationExecution'].sort());
  }

  async assertIncomingProcTransaccionesReady(): Promise<void> {
    if (this.postgres) {
      await this.postgres.assertIncomingProcTransaccionesSchema();
      return;
    }

    this.sqlServer!.assertReady();
    this.sqlServer!.assertIncomingProcTransaccionesSchema();
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

  async findIncomingNachaIngestion(criteria: { correlationId?: string; uniqueRunKey?: string }): Promise<IncomingNachaIngestionRow | null> {
    if (!criteria.correlationId && !criteria.uniqueRunKey) {
      throw new Error('La consulta de ingestión NACHA entrante requiere correlationId o uniqueRunKey.');
    }

    if (this.postgres) {
      const rows = await this.postgres.query<IncomingNachaIngestionRow>(
        `SELECT DISTINCT i."Id"::text AS id,
                i."FileName" AS "fileName",
                i."CorrelationId" AS "correlationId",
                i."IngestionStatus" AS "ingestionStatus",
                i."CycleResolutionStatus" AS "cycleResolutionStatus",
                i."ParsingStatus" AS "parsingStatus",
                i."ResolvedAchCycleId" AS "resolvedAchCycleId",
                i."ResolvedClearingHouseId" AS "resolvedClearingHouseId",
                i."OperationalDate" AS "operationalDate",
                i."UploadedAtUtc" AS "uploadedAtUtc"
         FROM "IncomingNachaFileIngestions" i
         LEFT JOIN "NachaHeaders" h ON h."IncomingNachaFileIngestionId" = i."Id"
         LEFT JOIN "AddendaRecords" a ON a."NachaID" = h."NachaID"
         WHERE ($1::text IS NULL OR i."CorrelationId" = $1)
           AND ($2::text IS NULL OR a."InvoiceOrAccountNumber" = $2)`,
        [criteria.correlationId ?? null, criteria.uniqueRunKey ?? null]
      );
      return singleOrNull(rows, 'La correlación NACHA entrante PostgreSQL debe identificar una sola ingestión.');
    }

    const predicates = [
      criteria.correlationId ? `i.[CorrelationId] = ${sqlString(criteria.correlationId)}` : '',
      criteria.uniqueRunKey ? `a.[InvoiceOrAccountNumber] = ${sqlString(criteria.uniqueRunKey)}` : ''
    ].filter(Boolean).join(' AND ');
    const rows = this.sqlQuery<IncomingNachaIngestionRow>(
      `SELECT DISTINCT i.[Id] AS [id],
              i.[FileName] AS [fileName],
              i.[CorrelationId] AS [correlationId],
              i.[IngestionStatus] AS [ingestionStatus],
              i.[CycleResolutionStatus] AS [cycleResolutionStatus],
              i.[ParsingStatus] AS [parsingStatus],
              i.[ResolvedAchCycleId] AS [resolvedAchCycleId],
              i.[ResolvedClearingHouseId] AS [resolvedClearingHouseId],
              i.[OperationalDate] AS [operationalDate],
              i.[UploadedAtUtc] AS [uploadedAtUtc]
       FROM [IncomingNachaFileIngestions] i
       LEFT JOIN [NachaHeaders] h ON h.[IncomingNachaFileIngestionId] = i.[Id]
       LEFT JOIN [AddendaRecords] a ON a.[NachaID] = h.[NachaID]
       WHERE ${predicates}`
    );
    return singleOrNull(rows, 'La correlación NACHA entrante SQL Server debe identificar una sola ingestión.');
  }

  async findIncomingDispatchQueueItem(ingestionId: string): Promise<IncomingNachaDispatchQueueRow | null> {
    if (this.postgres) {
      const rows = await this.postgres.query<IncomingNachaDispatchQueueRow>(
        `SELECT q."Id"::text AS id,
                q."IncomingNachaFileIngestionId"::text AS "incomingNachaFileIngestionId",
                q."AchCycleId" AS "achCycleId",
                q."QueueStatus" AS "queueStatus",
                q."AttemptCount" AS "attemptCount",
                q."LastErrorCode" AS "lastErrorCode",
                q."LastErrorMessage" AS "lastErrorMessage",
                q."LastResponseCode" AS "lastResponseCode",
                q."IdempotencyDispatchKey" AS "idempotencyDispatchKey",
                i."CorrelationId" AS "correlationId",
                c."FunctionalClass" AS "functionalClass",
                c."EligibilityStatus" AS "eligibilityStatus",
                q."CreatedAt" AS "createdAt",
                q."UpdatedAt" AS "updatedAt"
         FROM "IncomingNachaDispatchQueue" q
         JOIN "IncomingNachaFileIngestions" i ON i."Id" = q."IncomingNachaFileIngestionId"
         JOIN "IncomingNachaEntryClassifications" c ON c."Id" = q."IncomingNachaEntryClassificationId"
         WHERE q."IncomingNachaFileIngestionId" = $1::uuid`,
        [ingestionId]
      );
      return singleOrNull(rows, 'La ingestión NACHA entrante PostgreSQL debe producir una sola fila de cola para este fixture.');
    }

    const rows = this.sqlQuery<IncomingNachaDispatchQueueRow>(
      `SELECT q.[Id] AS [id],
              q.[IncomingNachaFileIngestionId] AS [incomingNachaFileIngestionId],
              q.[AchCycleId] AS [achCycleId],
              q.[QueueStatus] AS [queueStatus],
              q.[AttemptCount] AS [attemptCount],
              q.[LastErrorCode] AS [lastErrorCode],
              q.[LastErrorMessage] AS [lastErrorMessage],
              q.[LastResponseCode] AS [lastResponseCode],
              q.[IdempotencyDispatchKey] AS [idempotencyDispatchKey],
              i.[CorrelationId] AS [correlationId],
              c.[FunctionalClass] AS [functionalClass],
              c.[EligibilityStatus] AS [eligibilityStatus],
              q.[CreatedAt] AS [createdAt],
              q.[UpdatedAt] AS [updatedAt]
       FROM [IncomingNachaDispatchQueue] q
       JOIN [IncomingNachaFileIngestions] i ON i.[Id] = q.[IncomingNachaFileIngestionId]
       JOIN [IncomingNachaEntryClassifications] c ON c.[Id] = q.[IncomingNachaEntryClassificationId]
       WHERE q.[IncomingNachaFileIngestionId] = ${sqlString(ingestionId)}`
    );
    return singleOrNull(rows, 'La ingestión NACHA entrante SQL Server debe producir una sola fila de cola para este fixture.');
  }

  async findIncomingProcTransaccionesEvidence(
    lookup: string | { dispatchQueueId?: string; correlationId?: string; uniqueRunKey?: string }
  ): Promise<IncomingNachaIntegrationEvidenceRow | null> {
    const dispatchQueueId = typeof lookup === 'string' ? lookup : lookup.dispatchQueueId;
    if (!dispatchQueueId) {
      if (typeof lookup === 'string') {
        throw new Error('DispatchQueueId no puede ser vacio para consultar evidencia Proc_Transacciones.');
      }
      const ingestion = await this.findIncomingNachaIngestion({
        correlationId: lookup.correlationId,
        uniqueRunKey: lookup.uniqueRunKey
      });
      if (!ingestion) {
        return null;
      }
      const queue = await this.findIncomingDispatchQueueItem(ingestion.id);
      return queue ? this.findIncomingProcTransaccionesEvidence(queue.id) : null;
    }

    if (this.postgres) {
      const rows = await this.postgres.query<IncomingNachaIntegrationEvidenceRow>(
        `SELECT e."Id"::text AS id,
                e."DispatchQueueId"::text AS "dispatchQueueId",
                e."MethodName" AS "integrationType",
                e."SoapMethodName" AS "soapMethodName",
                e."SoapEndpoint" AS "soapEndpoint",
                e."ExecutionMode" AS "executionMode",
                e."RequestPayloadXml" AS "requestPayloadXml",
                e."ResponsePayloadXml" AS "responsePayloadXml",
                e."SoapResponseCode" AS "soapResponseCode",
                e."SoapResponseDescription" AS "soapResponseDescription",
                e."SoapTechnicalStatus" AS "soapTechnicalStatus",
                e."IsSuccessful" AS "isSuccessful",
                e."IsFunctionalRejection" AS "isFunctionalRejection",
                e."IsTechnicalFailure" AS "isTechnicalFailure",
                e."TechnicalException" AS "technicalException",
                e."CorrelationId" AS "correlationId",
                e."StartedAtUtc" AS "startedAtUtc",
                e."FinishedAtUtc" AS "finishedAtUtc",
                e."DurationMs" AS "durationMs"
         FROM "IncomingNachaIntegrationExecution" e
         WHERE e."DispatchQueueId" = $1::uuid
         ORDER BY e."StartedAtUtc" DESC
         LIMIT 1`,
        [dispatchQueueId]
      );
      return rows[0] ?? null;
    }

    const rows = this.sqlQuery<IncomingNachaIntegrationEvidenceRow>(
      `SELECT TOP (1) e.[Id] AS [id],
              e.[DispatchQueueId] AS [dispatchQueueId],
              e.[MethodName] AS [integrationType],
              e.[SoapMethodName] AS [soapMethodName],
              e.[SoapEndpoint] AS [soapEndpoint],
              e.[ExecutionMode] AS [executionMode],
              e.[RequestPayloadXml] AS [requestPayloadXml],
              e.[ResponsePayloadXml] AS [responsePayloadXml],
              e.[SoapResponseCode] AS [soapResponseCode],
              e.[SoapResponseDescription] AS [soapResponseDescription],
              e.[SoapTechnicalStatus] AS [soapTechnicalStatus],
              e.[IsSuccessful] AS [isSuccessful],
              e.[IsFunctionalRejection] AS [isFunctionalRejection],
              e.[IsTechnicalFailure] AS [isTechnicalFailure],
              e.[TechnicalException] AS [technicalException],
              e.[CorrelationId] AS [correlationId],
              e.[StartedAtUtc] AS [startedAtUtc],
              e.[FinishedAtUtc] AS [finishedAtUtc],
              e.[DurationMs] AS [durationMs]
       FROM [IncomingNachaIntegrationExecution] e
       WHERE e.[DispatchQueueId] = ${sqlString(dispatchQueueId)}
       ORDER BY e.[StartedAtUtc] DESC`
    );
    return rows[0] ?? null;
  }

  async cleanupIncomingProcTransaccionesRun(ingestionId: string): Promise<void> {
    if (this.postgres) {
      await this.postgres.executeTransaction(incomingCleanupCommandsPostgres(ingestionId));
      return;
    }

    this.sqlExecute(incomingCleanupSqlServer(ingestionId));
  }

  async accelerateIncomingPostProcessing(): Promise<IncomingPostProcessingTaskSnapshot> {
    const code = 'IncomingNachaPostProcessing';
    if (this.postgres) {
      const snapshot = await this.postgres.snapshotTask(code);
      await this.postgres.accelerateTask(code);
      return snapshot;
    }

    const snapshot = this.loadIncomingPostProcessingTaskSqlServer(code);
    this.sqlExecute(
      `UPDATE [TaskDefinition]
       SET [Status] = 1, [CalendarPolicy] = 0, [PeriodicityType] = 1, [N] = 1,
           [Minute] = NULL, [TimeOfDayTicks] = NULL, [WeeklyDay] = NULL, [MonthDay] = NULL,
           [CronExpression] = NULL, [StartAt] = DATEADD(minute, -1, SYSUTCDATETIME()), [EndAt] = NULL,
           [UpdatedAt] = SYSUTCDATETIME()
       WHERE [Id] = ${snapshot.id}`
    );
    return snapshot;
  }

  async restoreIncomingPostProcessing(snapshot: IncomingPostProcessingTaskSnapshot): Promise<void> {
    if (this.postgres) {
      await this.postgres.restoreTask(snapshot);
      return;
    }

    this.sqlExecute(
      `UPDATE [TaskDefinition]
       SET [Status] = ${snapshot.status}, [CalendarPolicy] = ${snapshot.calendarPolicy},
           [PeriodicityType] = ${snapshot.periodicityType}, [N] = ${sqlNullableNumber(snapshot.n)},
           [Minute] = ${sqlNullableNumber(snapshot.minute)}, [TimeOfDayTicks] = ${sqlNullableString(snapshot.timeOfDayTicks)},
           [WeeklyDay] = ${sqlNullableNumber(snapshot.weeklyDay)}, [MonthDay] = ${sqlNullableNumber(snapshot.monthDay)},
           [CronExpression] = ${sqlNullableString(snapshot.cronExpression)},
           [StartAt] = ${sqlNullableString(snapshot.startAt == null ? null : String(snapshot.startAt))},
           [EndAt] = ${sqlNullableString(snapshot.endAt == null ? null : String(snapshot.endAt))},
           [UpdatedAt] = SYSUTCDATETIME()
       WHERE [Id] = ${snapshot.id}`
    );
  }

  private loadIncomingPostProcessingTaskSqlServer(code: string): IncomingPostProcessingTaskSnapshot {
    const rows = this.sqlQuery<IncomingPostProcessingTaskSnapshot>(
      `SELECT [Id] AS [id], [Status] AS [status], [CalendarPolicy] AS [calendarPolicy],
              [PeriodicityType] AS [periodicityType], [N] AS [n], [Minute] AS [minute],
              [TimeOfDayTicks] AS [timeOfDayTicks], [WeeklyDay] AS [weeklyDay], [MonthDay] AS [monthDay],
              [CronExpression] AS [cronExpression], [StartAt] AS [startAt], [EndAt] AS [endAt]
       FROM [TaskDefinition]
       WHERE [Code] = ${sqlString(code)}`
    );
    expect(rows, `Debe existir TaskDefinition ${code}.`).toHaveLength(1);
    return rows[0];
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

function singleOrNull<T>(rows: T[], message: string): T | null {
  expect(rows.length, message).toBeLessThanOrEqual(1);
  return rows[0] ?? null;
}

function incomingCleanupCommandsPostgres(ingestionId: string): SqlCommand[] {
  const scope = '$1::uuid';
  const fileName = `(SELECT "FileName" FROM "IncomingNachaFileIngestions" WHERE "Id" = ${scope})`;
  return [
    { sql: `DELETE FROM "ExternalFileNameValidationLog" WHERE "RegistryId" IN (SELECT "Id" FROM "ExternalFileNameRegistry" WHERE "ExternalFileName" = ${fileName})`, values: [ingestionId] },
    { sql: `DELETE FROM "ExternalFileNameRegistry" WHERE "ExternalFileName" = ${fileName}`, values: [ingestionId] },
    { sql: `DELETE FROM "IncomingNachaIntegrationExecution" WHERE "DispatchQueueId" IN (SELECT "Id" FROM "IncomingNachaDispatchQueue" WHERE "IncomingNachaFileIngestionId" = ${scope})`, values: [ingestionId] },
    { sql: `DELETE FROM "IncomingNachaProcessingEvents" WHERE "IncomingNachaFileIngestionId" = ${scope}`, values: [ingestionId] },
    { sql: `DELETE FROM "IncomingNachaDispatchQueue" WHERE "IncomingNachaFileIngestionId" = ${scope}`, values: [ingestionId] },
    { sql: `DELETE FROM "IncomingNachaTransactionLinks" WHERE "IncomingNachaFileIngestionId" = ${scope}`, values: [ingestionId] },
    { sql: `DELETE FROM "IncomingNachaEntryClassifications" WHERE "IncomingNachaFileIngestionId" = ${scope}`, values: [ingestionId] },
    { sql: `DELETE FROM "IncomingNachaFileProcessingResults" WHERE "IncomingNachaFileIngestionId" = ${scope}`, values: [ingestionId] },
    { sql: `DELETE FROM "AddendaRecords" WHERE "NachaID" IN (SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" = ${scope})`, values: [ingestionId] },
    { sql: `DELETE FROM "BatchControls" WHERE "NachaID" IN (SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" = ${scope})`, values: [ingestionId] },
    { sql: `DELETE FROM "BatchHeaders" WHERE "NachaID" IN (SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" = ${scope})`, values: [ingestionId] },
    { sql: `DELETE FROM "EntryDetails" WHERE "NachaID" IN (SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" = ${scope})`, values: [ingestionId] },
    { sql: `DELETE FROM "FileControls" WHERE "NachaID" IN (SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" = ${scope})`, values: [ingestionId] },
    { sql: `DELETE FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" = ${scope}`, values: [ingestionId] },
    { sql: `DELETE FROM "IncomingNachaFileIngestions" WHERE "Id" = ${scope}`, values: [ingestionId] }
  ];
}

function incomingCleanupSqlServer(ingestionId: string): string {
  const id = sqlString(ingestionId);
  const headers = `(SELECT [NachaID] FROM [NachaHeaders] WHERE [IncomingNachaFileIngestionId] = ${id})`;
  const queues = `(SELECT [Id] FROM [IncomingNachaDispatchQueue] WHERE [IncomingNachaFileIngestionId] = ${id})`;
  const fileName = `(SELECT [FileName] FROM [IncomingNachaFileIngestions] WHERE [Id] = ${id})`;
  return `
    DELETE FROM [ExternalFileNameValidationLog] WHERE [RegistryId] IN (SELECT [Id] FROM [ExternalFileNameRegistry] WHERE [ExternalFileName] = ${fileName});
    DELETE FROM [ExternalFileNameRegistry] WHERE [ExternalFileName] = ${fileName};
    DELETE FROM [IncomingNachaIntegrationExecution] WHERE [DispatchQueueId] IN ${queues};
    DELETE FROM [IncomingNachaProcessingEvents] WHERE [IncomingNachaFileIngestionId] = ${id};
    DELETE FROM [IncomingNachaDispatchQueue] WHERE [IncomingNachaFileIngestionId] = ${id};
    DELETE FROM [IncomingNachaTransactionLinks] WHERE [IncomingNachaFileIngestionId] = ${id};
    DELETE FROM [IncomingNachaEntryClassifications] WHERE [IncomingNachaFileIngestionId] = ${id};
    DELETE FROM [IncomingNachaFileProcessingResults] WHERE [IncomingNachaFileIngestionId] = ${id};
    DELETE FROM [AddendaRecords] WHERE [NachaID] IN ${headers};
    DELETE FROM [BatchControls] WHERE [NachaID] IN ${headers};
    DELETE FROM [BatchHeaders] WHERE [NachaID] IN ${headers};
    DELETE FROM [EntryDetails] WHERE [NachaID] IN ${headers};
    DELETE FROM [FileControls] WHERE [NachaID] IN ${headers};
    DELETE FROM [NachaHeaders] WHERE [IncomingNachaFileIngestionId] = ${id};
    DELETE FROM [IncomingNachaFileIngestions] WHERE [Id] = ${id};`;
}
