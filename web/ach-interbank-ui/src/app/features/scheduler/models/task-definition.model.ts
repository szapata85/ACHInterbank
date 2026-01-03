export interface TaskParameterDto {
  id: number;
  key: string;
  value: string;
}

export interface TaskDefinitionDto {
  id: number;
  code: string;
  name: string;
  status: number;
  calendarPolicy: number;
  timeZoneId?: string | null;
  concurrencyPolicy: number;
  retryOnFailure: boolean;
  maxRetries?: number | null;
  retryBackoffSeconds: number;
  periodicityType: number;
  n?: number | null;
  minute?: number | null;
  timeOfDay?: string | null;
  weeklyDay?: number | null;
  monthDay?: number | null;
  cronExpression?: string | null;
  startAt?: string | null;
  endAt?: string | null;
  parameters: TaskParameterDto[];
}
