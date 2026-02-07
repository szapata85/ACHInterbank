export type CustomerThirdPartyStatus = 'Pending' | 'Active' | 'Rejected';

export interface CustomerThirdPartyRow {
  id: number;
  customerId: number;
  customerName: string;
  destinationInstitutionName: string;
  destinationAccountNumber: string;
  recipientIdNumber: string;
  status: CustomerThirdPartyStatus;
  prenotificationTransactionId?: number | null;
  validationCycleId?: string | null;
  validationReceivedAt?: string | null;
  validationMessage?: string | null;
}

export interface CustomerThirdPartyFilters {
  search?: string | null;
  destinationAccountNumber?: string | null;
  recipientIdNumber?: string | null;
  status?: CustomerThirdPartyStatus | null;
  page: number;
  pageSize: number;
}

export interface PagedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface UpdateCustomerThirdPartyStatusRequest {
  status: CustomerThirdPartyStatus;
  validationMessage?: string | null;
}
