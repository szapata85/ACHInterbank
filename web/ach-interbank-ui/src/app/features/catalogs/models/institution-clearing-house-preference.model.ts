export interface InstitutionClearingHousePreference {
  id: number;
  financialInstitutionId: number;
  financialInstitutionName: string;
  clearingHouseId: number;
  clearingHouseName: string;
  isDefault: boolean;
  priority: number;
}
