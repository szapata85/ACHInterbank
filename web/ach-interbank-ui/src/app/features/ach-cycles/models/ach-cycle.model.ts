export interface ClearingHouseOption {
  id: number;
  name: string;
  code?: string;
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
  status: string;
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
  date: string;
  startTime: string;
  endTime: string;
  status: string;
}

export interface PagedAchCycleResponse {
  items: AchCycleSummary[];
  total: number;
  page: number;
  pageSize: number;
}
