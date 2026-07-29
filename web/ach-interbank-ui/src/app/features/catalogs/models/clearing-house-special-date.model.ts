export interface ClearingHouseSpecialDate {
  id: number;
  clearingHouseId: number;
  clearingHouseName?: string | null;
  date: string;
  description: string;
  isActive: boolean;
  updatedAt?: string | null;
}
