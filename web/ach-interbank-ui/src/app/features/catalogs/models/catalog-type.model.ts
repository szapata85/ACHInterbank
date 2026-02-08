export interface CatalogTypeItem {
  code: string;
  name: string;
  description?: string | null;
}

export interface CatalogTypeUpsertRequest {
  code: string;
  name: string;
  description?: string | null;
}
