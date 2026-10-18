export interface AchColombiaOperationalScheduleAdministrationRow {
  clearingHouseId: number;
  clearingHouseCycleConfigId: number;
  policyVersion: string;
  cycleName: string;
  regulatoryStartTime: string;
  regulatoryCutoffTime: string;
  regulatoryCycleActive: boolean;
  scheduleId?: number | null;
  automaticOutboundEnabled: boolean;
  automaticOutboundStartTime?: string | null;
  automaticOutboundLatestStartTime?: string | null;
  outboundExecutionStrategy: number;
  effectiveTimeZoneId: string;
  isActive: boolean;
  rowVersion?: string | null;
}

export interface AchColombiaOperationalSchedule {
  id: number;
  clearingHouseId: number;
  clearingHouseCode: string;
  clearingHouseCycleConfigId: number;
  policyVersion: string;
  cycleName: string;
  automaticOutboundEnabled: boolean;
  automaticOutboundStartTime?: string | null;
  automaticOutboundLatestStartTime?: string | null;
  outboundExecutionStrategy: number;
  effectiveTimeZoneId: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}

export interface SaveAchColombiaOperationalSchedule {
  clearingHouseId: number;
  clearingHouseCycleConfigId: number;
  automaticOutboundEnabled: boolean;
  automaticOutboundStartTime?: string | null;
  automaticOutboundLatestStartTime?: string | null;
  outboundExecutionStrategy: number;
  effectiveTimeZoneId: string;
  isActive: boolean;
  expectedRowVersion?: string;
}
