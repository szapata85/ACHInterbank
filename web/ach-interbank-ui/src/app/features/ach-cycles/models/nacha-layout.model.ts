export interface NachaRecordFieldDto {
  id: number;
  fieldName: string;
  startPosition: number;
  length: number;
  padChar: string;
  justification: string;
  dbColumn: string;
  format?: string | null;
}

export interface NachaRecordLayoutDto {
  id: number;
  recordType: string;
  recordCode: string;
  totalLength: number;
  description?: string | null;
  fields: NachaRecordFieldDto[];
}
