export interface NachaRecordDefinitionDto {
  id: number;
  recordCode: string;
  sequence: number;
  sourceType: number;
  sourceName?: string | null;
  filterKey?: string | null;
  isEnabled: boolean;
}
