export interface ClearingHouseOption {
  id: string;
  name: string;
}

export interface AchCycleSummary {
  id: string;
  clearingHouseId: string;
  clearingHouseName: string;
  date: string;
  startTime: string;
  endTime: string;
  status: string;
}

export interface AchCycleFilter {
  clearingHouseId?: string;
  date?: string;
  page?: number;
  pageSize?: number;
}

export interface SaveAchCycleRequest {
  clearingHouseId: string;
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
