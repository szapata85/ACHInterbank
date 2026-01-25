export interface CustomerSummary {
  id: number;
  documentType: string;
  documentNumber: string;
  accountNumber: string;
  personType: string;
  companyName?: string | null;
  fullName: string;
}

export interface CustomerDetail {
  id: number;
  firstName: string;
  middleName?: string | null;
  lastName: string;
  secondLastName?: string | null;
  gender?: string | null;
  personType: string;
  companyName?: string | null;
  documentType: string;
  documentNumber: string;
  accountNumber: string;
}

export interface SaveCustomerRequest {
  firstName: string;
  middleName?: string | null;
  lastName: string;
  secondLastName?: string | null;
  gender?: string | null;
  personType: string;
  companyName?: string | null;
  documentType: string;
  documentNumber: string;
  accountNumber: string;
}
