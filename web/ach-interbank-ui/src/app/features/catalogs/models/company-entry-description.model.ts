export interface CompanyEntryDescriptionItem {
  id: number;
  term: string;
  description: string;
  standardEntryClassCode: 'PPD' | 'CCD';
  isActive: boolean;
}

export interface CompanyEntryDescriptionUpsertRequest {
  term: string;
  description: string;
  standardEntryClassCode: 'PPD' | 'CCD';
  isActive: boolean;
}
