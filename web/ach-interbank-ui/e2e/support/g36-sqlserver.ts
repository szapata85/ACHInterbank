import { expect } from '@playwright/test';
import { execFileSync } from 'node:child_process';

export type G36SqlServerOptions = {
  connectionString?: string;
  host?: string;
  port?: string;
  database?: string;
  user?: string;
  password?: string;
  sqlcmdPath?: string;
};

export class G36SqlServer {
  private readonly config: Required<Omit<G36SqlServerOptions, 'connectionString'>>;

  constructor(options: G36SqlServerOptions = {}) {
    const fromConnectionString = parseSqlServerConnectionString(
      options.connectionString ?? process.env['ACH_E2E_SQLSERVER_CONNECTION_STRING']
    );

    const host = options.host
      ?? process.env['ACH_E2E_SQLSERVER_HOST']
      ?? fromConnectionString.host;
    const port = options.port
      ?? process.env['ACH_E2E_SQLSERVER_PORT']
      ?? fromConnectionString.port;
    const database = options.database
      ?? process.env['ACH_E2E_SQLSERVER_DATABASE']
      ?? fromConnectionString.database;
    const user = options.user
      ?? process.env['ACH_E2E_SQLSERVER_USER']
      ?? fromConnectionString.user;
    const password = options.password
      ?? process.env['ACH_E2E_SQLSERVER_PASSWORD']
      ?? fromConnectionString.password;
    const sqlcmdPath = options.sqlcmdPath
      ?? process.env['ACH_E2E_SQLCMD_PATH']
      ?? process.env['SQLCMD_PATH']
      ?? 'sqlcmd';

    if (!host || !port || !database || !user || !password) {
      throw new Error([
        'SQL Server E2E requiere ACH_E2E_SQLSERVER_CONNECTION_STRING',
        'o ACH_E2E_SQLSERVER_HOST/PORT/DATABASE/USER/PASSWORD.',
        'Puede reutilizar valores locales de docker-compose.sqlserver.yml exportandolos al entorno, sin hardcodearlos en el spec.'
      ].join(' '));
    }

    this.config = { host, port, database, user, password, sqlcmdPath };
  }

  assertReady(): void {
    const database = this.scalar<string>('SELECT DB_NAME() AS [value]');
    expect(database, 'SQL Server runtime debe estar disponible.').toBeTruthy();
  }

  query<T>(selectSql: string): T[] {
    const output = this.run(`${selectSql} FOR JSON PATH, INCLUDE_NULL_VALUES;`);
    return parseSqlJson<T>(output);
  }

  execute(sql: string): void {
    this.run(sql);
  }

  scalar<T>(selectSql: string): T | null {
    const rows = this.query<{ value: T }>(selectSql);
    return rows[0]?.value ?? null;
  }

  close(): void {
    // sqlcmd opens a short-lived process per command.
  }

  private run(sql: string, failOnError = true): string {
    const args = [
      '-S', `${this.config.host},${this.config.port}`,
      '-U', this.config.user,
      '-P', this.config.password,
      '-d', this.config.database,
      '-C',
      '-w', '65535',
      '-y', '0',
      '-Q', `SET NOCOUNT ON; ${sql}`
    ];

    if (failOnError) {
      args.unshift('-b');
    }

    try {
      return execFileSync(this.config.sqlcmdPath, args, {
        encoding: 'utf8',
        windowsHide: true,
        maxBuffer: 10 * 1024 * 1024
      });
    } catch (error) {
      const details = error instanceof Error ? sanitizeErrorMessage(error.message) : String(error);
      throw new Error([
        `sqlcmd fallo contra ${this.config.host},${this.config.port}/${this.config.database}.`,
        details
      ].join(' '));
    }
  }
}

export function sqlString(value: string): string {
  return `N'${value.replace(/'/g, "''")}'`;
}

export function sqlNullableString(value: string | null | undefined): string {
  return value == null ? 'NULL' : sqlString(value);
}

export function sqlNullableNumber(value: number | null | undefined): string {
  return value == null ? 'NULL' : String(value);
}

export function sqlNullableBoolean(value: boolean | null | undefined): string {
  return value == null ? 'NULL' : (value ? '1' : '0');
}

function parseSqlJson<T>(output: string): T[] {
  const start = output.indexOf('[');
  const end = output.lastIndexOf(']');
  if (start < 0 || end < start) {
    return [];
  }

  return JSON.parse(output.slice(start, end + 1).replace(/\r?\n/g, '')) as T[];
}

function parseSqlServerConnectionString(connectionString: string | undefined): Partial<{
  host: string;
  port: string;
  database: string;
  user: string;
  password: string;
}> {
  if (!connectionString) {
    return {};
  }

  const values = new Map<string, string>();
  for (const part of connectionString.split(';')) {
    const separator = part.indexOf('=');
    if (separator <= 0) {
      continue;
    }

    values.set(part.slice(0, separator).trim().toLowerCase(), part.slice(separator + 1).trim());
  }

  const server = values.get('server') ?? values.get('data source') ?? '';
  const [host, port] = splitSqlServerHost(server);
  return {
    host,
    port,
    database: values.get('database') ?? values.get('initial catalog'),
    user: values.get('user id') ?? values.get('uid') ?? values.get('user'),
    password: values.get('password') ?? values.get('pwd')
  };
}

function splitSqlServerHost(server: string): [string | undefined, string | undefined] {
  if (!server) {
    return [undefined, undefined];
  }

  const normalized = server.replace(/^tcp:/i, '');
  const [host, port] = normalized.split(',');
  return [host || undefined, port || '1433'];
}

function sanitizeErrorMessage(message: string): string {
  return message
    .replace(/(-P\s+)(?:"[^"]+"|'[^']+'|\S+)/gi, '$1<redacted>')
    .replace(/(-U\s+)(?:"[^"]+"|'[^']+'|\S+)/gi, '$1<redacted>')
    .replace(/(Password=)[^;\s]+/gi, '$1<redacted>')
    .replace(/(User Id=)[^;\s]+/gi, '$1<redacted>');
}
