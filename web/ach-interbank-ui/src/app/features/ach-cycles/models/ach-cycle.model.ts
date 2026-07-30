export interface ClearingHouseOption {
  id: number;
  name: string;
  code?: string;
  isActive?: boolean;
  requiresNachaProfile?: boolean;
  isReady?: boolean;
  missingRequirements?: string[];
}

export interface AchCycleSummary {
  id: string;
  cycleName: string;
  clearingHouseId: number;
  clearingHouseName: string;
  date?: string;
  processingDate?: string;
  startTime: string;
  endTime: string;
  cutoffTime: string;
  rescheduleOnHoliday: boolean;
  clearingHouseCycleConfigId: number | null;
  operationalStatus: string;
  status?: string;
}

export interface AchCycleConfigurationOption {
  id: number;
  clearingHouseId: number;
  clearingHouseName?: string;
  cycleName: string;
  startTime: string;
  endTime: string;
  cutoffTime: string;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isCurrent: boolean;
}

export interface AchCycleFilter {
  clearingHouseId?: number;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
}

export interface SaveAchCycleRequest {
  clearingHouseId: number;
  clearingHouseCycleConfigId: number;
  cycleName: string;
  processingDate: string;
  startTime: string;
  endTime: string;
  cutoffTime: string;
  rescheduleOnHoliday: boolean;
}

export interface PagedAchCycleResponse {
  items: AchCycleSummary[];
  total: number;
  page: number;
  pageSize: number;
}
