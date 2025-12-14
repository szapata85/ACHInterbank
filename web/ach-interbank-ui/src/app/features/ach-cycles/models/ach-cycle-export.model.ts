export interface ExportableAchCycle {
  id: string;
  cycleName: string;
  processingDate: string;
  clearingHouseName?: string;
  transactionCount: number;
}
