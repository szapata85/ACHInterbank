export interface ExportableAchCycle {
  id: number;
  cycleName: string;
  processingDate: string;
  clearingHouseName?: string;
  transactionCount: number;
}
