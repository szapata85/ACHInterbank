import { expect } from '@playwright/test';
import { Pool } from 'pg';

export type TaskDefinitionSnapshot = {
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
  startAt: Date | null;
  endAt: Date | null;
  updatedAt: Date;
};

export type AchCycleSnapshot = {
  id: string;
  cycleName: string;
  processingDate: Date;
  cutoffTime: string;
  startTime: string;
  endTime: string;
  rescheduleOnHoliday: boolean;
  clearingHouseId: number;
  updatedAt: Date;
};

export type ClearingHouseOriginSnapshot = {
  id: number;
  originCode: string;
};

export type TaskExecutionEvidence = {
  id: string;
  taskDefinitionId: number;
  scheduledAt: Date;
  startedAt: Date;
  finishedAt: Date | null;
  success: boolean;
  error: string | null;
  output: string | null;
  executionKey: string;
};

export type SqlCommand = {
  sql: string;
  values?: readonly unknown[];
};

export type G36PostgresOptions = {
  connectionString?: string;
  host?: string;
  port?: number;
  database?: string;
  user?: string;
  password?: string;
  requireExplicitConfig?: boolean;
};

export class G36Postgres {
  private readonly pool: Pool;

  constructor(options: G36PostgresOptions = {}) {
    const connectionString = options.connectionString ?? process.env['ACH_E2E_POSTGRES_CONNECTION_STRING'];
    if (connectionString) {
      this.pool = new Pool({
        ...parsePostgresConnectionString(connectionString),
        max: 4,
        connectionTimeoutMillis: 10_000,
        idleTimeoutMillis: 10_000
      });
      return;
    }

    const host = options.host ?? process.env['ACH_E2E_POSTGRES_HOST'] ?? process.env['POSTGRES_HOST'];
    const port = options.port ?? readNumberEnv('ACH_E2E_POSTGRES_PORT', 'POSTGRES_PORT');
    const database = options.database ?? process.env['ACH_E2E_POSTGRES_DATABASE'] ?? process.env['POSTGRES_DB'];
    const user = options.user ?? process.env['ACH_E2E_POSTGRES_USER'] ?? process.env['POSTGRES_USER'];
    const password = options.password ?? process.env['ACH_E2E_POSTGRES_PASSWORD'] ?? process.env['POSTGRES_PASSWORD'];

    if (!host || !port || !database || !user || !password) {
      throw new Error([
        'PostgreSQL E2E requiere ACH_E2E_POSTGRES_CONNECTION_STRING o ACH_E2E_POSTGRES_HOST/PORT/DATABASE/USER/PASSWORD.',
        'Tambien puede reutilizar POSTGRES_* cargandolas desde .env local, sin commitear secretos.'
      ].join(' '));
    }

    this.pool = new Pool({
      host,
      port,
      database,
      user,
      password,
      max: 4,
      connectionTimeoutMillis: 10_000,
      idleTimeoutMillis: 10_000
    });
  }

  async close(): Promise<void> {
    await this.pool.end();
  }

  async assertReady(): Promise<void> {
    const database = await this.scalar<string>('SELECT current_database();');
    expect(database, 'PostgreSQL UAT debe estar disponible.').toBeTruthy();

    const requiredTables = [
      'AchCycles',
      'IncomingNachaFileIngestions',
      'IncomingNachaDispatchQueue',
      'ContrapartidaDispatchBatches',
      'TaskDefinition',
      'TaskExecutionLog'
    ];
    const rows = await this.query<{ table_name: string }>(
      `SELECT table_name
       FROM information_schema.tables
       WHERE table_schema = 'public' AND table_name = ANY($1::text[])`,
      [requiredTables]
    );
    expect(rows.map((row) => row.table_name).sort(), 'La base UAT debe estar provisionada antes de G3.6.')
      .toEqual([...requiredTables].sort());
  }

  async query<T>(sql: string, values: readonly unknown[] = []): Promise<T[]> {
    const result = await this.pool.query(sql, [...values]);
    return result.rows as T[];
  }

  async execute(sql: string, values: readonly unknown[] = []): Promise<number> {
    const result = await this.pool.query(sql, [...values]);
    return result.rowCount ?? 0;
  }

  async executeTransaction(commands: readonly SqlCommand[]): Promise<void> {
    const client = await this.pool.connect();
    try {
      await client.query('BEGIN');
      for (const command of commands) {
        await client.query(command.sql, [...(command.values ?? [])]);
      }
      await client.query('COMMIT');
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally {
      client.release();
    }
  }

  async scalar<T>(sql: string, values: readonly unknown[] = []): Promise<T | null> {
    const rows = await this.query<Record<string, T>>(sql, values);
    if (rows.length === 0) {
      return null;
    }

    const firstKey = Object.keys(rows[0])[0];
    return firstKey ? rows[0][firstKey] : null;
  }

  async snapshotTask(code: string): Promise<TaskDefinitionSnapshot> {
    const rows = await this.query<{
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
      startAt: Date | null;
      endAt: Date | null;
      updatedAt: Date;
    }>(
      `SELECT "Id" AS id,
              "Status" AS "status",
              "CalendarPolicy" AS "calendarPolicy",
              "PeriodicityType" AS "periodicityType",
              "N" AS n,
              "Minute" AS minute,
              "TimeOfDayTicks"::text AS "timeOfDayTicks",
              "WeeklyDay" AS "weeklyDay",
              "MonthDay" AS "monthDay",
              "CronExpression" AS "cronExpression",
              "StartAt" AS "startAt",
              "EndAt" AS "endAt",
              "UpdatedAt" AS "updatedAt"
       FROM "TaskDefinition"
       WHERE "Code" = $1`,
      [code]
    );
    expect(rows, `Debe existir TaskDefinition ${code}.`).toHaveLength(1);
    return rows[0];
  }

  async accelerateTask(code: string): Promise<TaskDefinitionSnapshot> {
    const snapshot = await this.snapshotTask(code);
    await this.execute(
      `UPDATE "TaskDefinition"
       SET "Status" = 1,
           "CalendarPolicy" = 0,
           "PeriodicityType" = 1,
           "N" = 1,
           "Minute" = NULL,
           "TimeOfDayTicks" = NULL,
           "WeeklyDay" = NULL,
           "MonthDay" = NULL,
           "CronExpression" = NULL,
           "StartAt" = NOW() - INTERVAL '1 minute',
           "EndAt" = NULL,
           "UpdatedAt" = NOW()
       WHERE "Id" = $1`,
      [snapshot.id]
    );
    return snapshot;
  }

  async pauseTask(code: string): Promise<void> {
    const changed = await this.execute(
      `UPDATE "TaskDefinition"
       SET "Status" = 0,
           "UpdatedAt" = NOW()
       WHERE "Code" = $1`,
      [code]
    );
    expect(changed, `Debe poder pausarse TaskDefinition ${code}.`).toBe(1);
  }

  async waitForSchedulerSyncCycle(): Promise<void> {
    await delay(65_000);
  }

  async restoreTask(snapshot: TaskDefinitionSnapshot): Promise<void> {
    await this.execute(
      `UPDATE "TaskDefinition"
       SET "Status" = $2,
           "CalendarPolicy" = $3,
           "PeriodicityType" = $4,
           "N" = $5,
           "Minute" = $6,
           "TimeOfDayTicks" = $7,
           "WeeklyDay" = $8,
           "MonthDay" = $9,
           "CronExpression" = $10,
           "StartAt" = $11,
           "EndAt" = $12,
           "UpdatedAt" = NOW()
       WHERE "Id" = $1`,
      [
        snapshot.id,
        snapshot.status,
        snapshot.calendarPolicy,
        snapshot.periodicityType,
        snapshot.n,
        snapshot.minute,
        snapshot.timeOfDayTicks,
        snapshot.weeklyDay,
        snapshot.monthDay,
        snapshot.cronExpression,
        snapshot.startAt,
        snapshot.endAt
      ]
    );
  }

  async taskExecutionBaseline(taskId: number): Promise<number> {
    return Number(await this.scalar<string>(
      `SELECT COALESCE(MAX("Id"), 0)::text
       FROM "TaskExecutionLog"
       WHERE "TaskDefinitionId" = $1`,
      [taskId]
    ) ?? 0);
  }

  async waitForTaskExecution(
    taskId: number,
    afterId: number,
    timeoutMs = 150_000
  ): Promise<TaskExecutionEvidence> {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
      const rows = await this.query<TaskExecutionEvidence>(
        `SELECT "Id"::text AS id,
                "TaskDefinitionId" AS "taskDefinitionId",
                "ScheduledAt" AS "scheduledAt",
                "StartedAt" AS "startedAt",
                "FinishedAt" AS "finishedAt",
                "Success" AS success,
                "Error" AS error,
                "Output" AS output,
                "ExecutionKey" AS "executionKey"
         FROM "TaskExecutionLog"
         WHERE "TaskDefinitionId" = $1
           AND "Id" > $2
           AND "FinishedAt" IS NOT NULL
         ORDER BY "Id" DESC
         LIMIT 1`,
        [taskId, afterId]
      );
      if (rows[0]) {
        return rows[0];
      }
      await delay(2_000);
    }

    throw new Error(`Quartz no produjo TaskExecutionLog para taskId=${taskId} dentro de ${timeoutMs} ms.`);
  }

  async findReusableCycle(clearingHouseCode = 'ACHCOL'): Promise<AchCycleSnapshot> {
    const rows = await this.query<AchCycleSnapshot>(
      `SELECT c."Id" AS id,
              c."CycleName" AS "cycleName",
              c."ProcessingDate" AS "processingDate",
              c."CutoffTime"::text AS "cutoffTime",
              c."StartTime"::text AS "startTime",
              c."EndTime"::text AS "endTime",
              c."RescheduleOnHoliday" AS "rescheduleOnHoliday",
              c."ClearingHouseId" AS "clearingHouseId",
              c."UpdatedAt" AS "updatedAt"
       FROM "AchCycles" c
       JOIN "ClearingHouses" ch ON ch."Id" = c."ClearingHouseId"
       WHERE UPPER(ch."Code") IN (UPPER($1), 'ACH')
          OR UPPER(ch."Name") = 'ACH COLOMBIA'
       ORDER BY c."ProcessingDate" DESC, c."Id"
       LIMIT 1`,
      [clearingHouseCode]
    );
    expect(rows.length, 'Debe existir un AchCycle reutilizable de ACH Colombia; G3.6 no crea ciclos.').toBe(1);
    return rows[0];
  }

  async configureCycle(
    snapshot: AchCycleSnapshot,
    cycleName: string,
    processingDate: string
  ): Promise<void> {
    await this.execute(
      `UPDATE "AchCycles"
       SET "CycleName" = $2,
           "ProcessingDate" = $3::date,
           "StartTime" = TIME '00:00:00',
           "EndTime" = TIME '23:59:59',
           "CutoffTime" = TIME '23:59:59',
           "RescheduleOnHoliday" = FALSE,
           "UpdatedAt" = NOW()
       WHERE "Id" = $1`,
      [snapshot.id, cycleName, processingDate]
    );
  }

  async configureClearingHouseOrigin(
    clearingHouseId: number,
    originCode: string
  ): Promise<ClearingHouseOriginSnapshot> {
    const rows = await this.query<ClearingHouseOriginSnapshot>(
      `SELECT "Id" AS id, "OriginCode" AS "originCode"
       FROM "ClearingHouses"
       WHERE "Id" = $1`,
      [clearingHouseId]
    );
    expect(rows, `Debe existir ClearingHouse ${clearingHouseId}.`).toHaveLength(1);
    const snapshot = rows[0];
    await this.execute(
      `UPDATE "ClearingHouses" SET "OriginCode" = $2 WHERE "Id" = $1`,
      [clearingHouseId, originCode]
    );
    return snapshot;
  }

  async restoreClearingHouseOrigin(snapshot: ClearingHouseOriginSnapshot): Promise<void> {
    await this.execute(
      `UPDATE "ClearingHouses" SET "OriginCode" = $2 WHERE "Id" = $1`,
      [snapshot.id, snapshot.originCode]
    );
  }

  async restoreCycle(snapshot: AchCycleSnapshot): Promise<void> {
    await this.execute(
      `UPDATE "AchCycles"
       SET "CycleName" = $2,
           "ProcessingDate" = $3,
           "CutoffTime" = $4::time,
           "StartTime" = $5::time,
           "EndTime" = $6::time,
           "RescheduleOnHoliday" = $7,
           "UpdatedAt" = NOW()
       WHERE "Id" = $1`,
      [
        snapshot.id,
        snapshot.cycleName,
        snapshot.processingDate,
        snapshot.cutoffTime,
        snapshot.startTime,
        snapshot.endTime,
        snapshot.rescheduleOnHoliday
      ]
    );
  }
}

export async function pollUntil<T>(
  action: () => Promise<T | null | undefined>,
  description: string,
  timeoutMs = 120_000
): Promise<T> {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const value = await action();
    if (value !== null && value !== undefined) {
      return value;
    }
    await delay(2_000);
  }

  throw new Error(`Timeout esperando ${description} (${timeoutMs} ms).`);
}

function delay(milliseconds: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}

function readNumberEnv(...names: string[]): number | undefined {
  for (const name of names) {
    const value = process.env[name];
    if (value) {
      return Number(value);
    }
  }

  return undefined;
}

function parsePostgresConnectionString(connectionString: string): {
  connectionString?: string;
  host?: string;
  port?: number;
  database?: string;
  user?: string;
  password?: string;
} {
  if (/^postgres(?:ql)?:\/\//i.test(connectionString)) {
    return { connectionString };
  }

  const values = new Map<string, string>();
  for (const part of connectionString.split(';')) {
    const separator = part.indexOf('=');
    if (separator <= 0) {
      continue;
    }

    values.set(part.slice(0, separator).trim().toLowerCase(), part.slice(separator + 1).trim());
  }

  return {
    host: values.get('host') ?? values.get('server'),
    port: values.get('port') ? Number(values.get('port')) : undefined,
    database: values.get('database') ?? values.get('dbname'),
    user: values.get('username') ?? values.get('user id') ?? values.get('user') ?? values.get('uid'),
    password: values.get('password') ?? values.get('pwd')
  };
}
