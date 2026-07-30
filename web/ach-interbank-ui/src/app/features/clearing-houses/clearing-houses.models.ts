export interface ClearingHouse {
  id: number;
  code: string;
  name: string;
  originCode: string;
  isActive: boolean;
  timeZoneId: string;
  holidayStrategy: string;
  paymentRailCode?: string | null;
  requiresNachaProfile: boolean;
  nachaProfileId?: number | null;
  nachaProfileCode?: string | null;
  nachaProfileName?: string | null;
  activeCycleCount: number;
  isReady: boolean;
  missingRequirements: string[];
  createdAt: string;
  updatedAt?: string | null;
}

export interface ClearingHousePage {
  items: ClearingHouse[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ClearingHouseInput {
  code: string;
  name: string;
  originCode: string;
  timeZoneId: string;
  holidayStrategy: string;
  paymentRailCode?: string | null;
  requiresNachaProfile: boolean;
  nachaProfileId?: number | null;
  expectedUpdatedAt?: string | null;
}

export interface NachaProfileOption {
  id: number;
  code: string;
  name: string;
  clearingHouseCode?: string;
  isPublished?: boolean;
  isCurrent?: boolean;
}
export interface PaymentRailOption { code: string; name: string; }
