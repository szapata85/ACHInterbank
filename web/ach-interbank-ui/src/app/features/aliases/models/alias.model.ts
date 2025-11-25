export interface AliasSummary {
  id: string;
  alias: string;
  accountNumber: string;
  documentNumber?: string;
  phoneNumber?: string;
  ownerName?: string;
  status: string;
}

export interface AliasFilter {
  search?: string;
  documentNumber?: string;
  phoneNumber?: string;
  page?: number;
  pageSize?: number;
}

export interface SaveAliasRequest {
  alias: string;
  accountNumber: string;
  documentNumber?: string;
  phoneNumber?: string;
  ownerName?: string;
}

export interface PagedAliasResponse {
  items: AliasSummary[];
  total: number;
  page: number;
  pageSize: number;
}
