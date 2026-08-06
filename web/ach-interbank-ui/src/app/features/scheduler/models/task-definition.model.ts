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

export interface SchedulerOverview {
  totalInstances: number;
  activeInstances: number;
  offlineInstances: number;
  runningJobs: number;
  upcomingExecutions: number;
  recentFailures: number;
  recentMisfires: number;
  schedulerName: string;
  persistentStore: boolean;
  clustered: boolean;
  pendingSynchronizations: number;
}

export interface SchedulerTask {
  taskCode: string;
  name: string;
  description: string;
  status: string;
  clearingHouse?: string | null;
  scheduleDescription: string;
  cronExpression?: string | null;
  timeZoneId: string;
  misfirePolicy: number;
  misfireDescription: string;
  lastExecutionUtc?: string | null;
  nextExecutionUtc?: string | null;
  lastResult?: string | null;
  lastDurationMilliseconds?: number | null;
  lastSchedulerInstance?: string | null;
  currentState: string;
  manualExecutionEnabled: boolean;
  requestsRecovery: boolean;
  allowsConcurrentExecution: boolean;
  periodicityType: number;
  n?: number | null;
  minute?: number | null;
  timeOfDay?: string | null;
  weeklyDay?: number | null;
  monthDay?: number | null;
  onlyBusinessDays: boolean;
  startAt?: string | null;
  endAt?: string | null;
  synchronizationStatus: string;
  lastSynchronizationError?: string | null;
  category: string;
  processType: string;
  soapService?: string | null;
  usesCycleSchedule: boolean;
  canEditSchedule: boolean;
  operationalContexts: SchedulerOperationalContext[];
}

export interface SchedulerOperationalContext {
  cycleConfigId: number;
  clearingHouseCode: string;
  clearingHouseName: string;
  cycleName: string;
  windowDescription: string;
  cutoffDescription: string;
  nextValidWindowUtc?: string | null;
  nextValidWindowEndUtc?: string | null;
  status: string;
}

export interface SchedulerTechnicalInfo {
  taskCode: string;
  handlerCode: string;
  soapService?: string | null;
  jobName: string;
  jobGroup: string;
  cronExpression?: string | null;
  timeZoneId: string;
  misfirePolicy: number;
  requestsRecovery: boolean;
  allowsConcurrentExecution: boolean;
  parameters: Record<string, string>;
  triggerKeys: string[];
}

export interface SchedulerExecution {
  executionId: string;
  taskCode: string;
  triggerType: string;
  schedulerInstanceId: string;
  schedulerInstanceName: string;
  requestedByUserName?: string | null;
  requestReason?: string | null;
  requestId?: string | null;
  correlationId: string;
  startedAtUtc: string;
  finishedAtUtc?: string | null;
  durationMilliseconds?: number | null;
  status: number;
  isRecovery: boolean;
  misfireDetected: boolean;
  resultSummary?: string | null;
  errorCode?: string | null;
  errorSummary?: string | null;
}

export interface SchedulerInstance {
  instanceId: string;
  instanceName: string;
  hostName: string;
  startedAtUtc: string;
  lastHeartbeatUtc: string;
  status: string;
  isCurrentInstance: boolean;
  currentlyExecutingJobs: number;
  version: string;
}

export interface SchedulerPagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface SchedulerScheduleRequest {
  periodicityType: number;
  n?: number | null;
  minute?: number | null;
  timeOfDay?: string | null;
  weeklyDay?: number | null;
  monthDay?: number | null;
  cronExpression?: string | null;
  timeZoneId: string;
  misfirePolicy: number;
  onlyBusinessDays: boolean;
  startAt?: string | null;
  endAt?: string | null;
}

export interface SchedulerSchedulePreview {
  description: string;
  nextExecutionsUtc: string[];
}

export interface ManualExecutionResult {
  outcome: number;
  executionId?: string | null;
  activeExecutionId?: string | null;
  message: string;
}
