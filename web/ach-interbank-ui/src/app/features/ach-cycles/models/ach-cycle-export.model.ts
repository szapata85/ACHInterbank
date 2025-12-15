export interface ExportableAchCycle {
  id: string;
  cycleName: string;
  processingDate: string;
  clearingHouseName?: string;
  transactionCount: number;
}

export interface ExportableAchCycleFilter {
  clearingHouseId?: number;
  startDate?: string;
  endDate?: string;
}
