export interface AchErrorCode {
  returnCode: string;
  category: 'Operator' | 'Receiving Entity';
  standardDescription: string;
  additionalDetail: string;
  applicability: 'Monetary' | 'Prenotification' | 'Both';
}
