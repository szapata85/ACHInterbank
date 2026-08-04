export interface ClearingHouseSpecialDate {
  id: number;
  clearingHouseId: number;
  clearingHouseName?: string | null;
  date: string;
  description: string;
  isActive: boolean;
  isWeekend?: boolean;
  isNationalHoliday?: boolean;
  calendarWarning?: string | null;
  createdAt?: string | null;
  updatedAt?: string | null;
}
