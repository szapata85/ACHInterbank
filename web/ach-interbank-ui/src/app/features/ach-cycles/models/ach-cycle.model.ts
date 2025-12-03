export interface ClearingHouseOption {
  id: number;
  name: string;
}

export interface AchCycleSummary {
  id: string;
  clearingHouseId: number;
  clearingHouseName: string;
  date: string;
  startTime: string;
  endTime: string;
  status: string;
}

export interface AchCycleFilter {
  clearingHouseId?: number;
  date?: string;
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
