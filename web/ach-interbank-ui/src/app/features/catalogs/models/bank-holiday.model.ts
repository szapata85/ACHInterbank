export interface BankHoliday {
  id: number;
  date: string;
  description: string;
  countryCode: string;
  commemorativeDate?: string | null;
  ruleCode?: string | null;
  ruleKind?: 'Fixed' | 'Emiliani' | 'Easter' | 'EasterEmiliani' | 'ChiquinquiraEmiliani' | null;
  isSystemGenerated?: boolean;
  legalOrigin?: string | null;
  effectiveFromYear?: number | null;
  wasMoved?: boolean;
}

const dateOnlyPattern = /^(\d{4})-(\d{2})-(\d{2})(?:T.*)?$/;

export function parseBankHolidayLocalDate(value: string | null | undefined): Date | null {
  const match = value?.match(dateOnlyPattern);
  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(year, month - 1, day);

  if (
    date.getFullYear() !== year
    || date.getMonth() !== month - 1
    || date.getDate() !== day
  ) {
    return null;
  }

  return date;
}

export function toBankHolidayDateOnly(value: Date | null | undefined): string {
  if (!value || Number.isNaN(value.getTime())) {
    return '';
  }

  const year = value.getFullYear().toString().padStart(4, '0');
  const month = (value.getMonth() + 1).toString().padStart(2, '0');
  const day = value.getDate().toString().padStart(2, '0');
  return `${year}-${month}-${day}`;
}
