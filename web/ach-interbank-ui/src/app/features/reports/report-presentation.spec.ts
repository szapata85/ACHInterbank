import { extractReportFileName, formatReportValue, humanizeReportValue, validatePdfBlob } from './report-presentation';

describe('report presentation', () => {
  it('humanizes contract values without changing them', () => {
    expect(humanizeReportValue('Pending')).toBe('Pendiente');
    expect(humanizeReportValue('ReturnedByOperator')).toBe('Devuelta por el operador');
    expect(humanizeReportValue('System')).toBe('Sistema');
  });

  it('formats amounts and date-only values for Colombia', () => {
    expect(String(formatReportValue('amount', 1250000))).toContain('1.250.000');
    expect(String(formatReportValue('effectiveEntryDate', '2026-08-13'))).toContain('2026');
  });

  it('validates the MIME type and PDF signature', async () => {
    const valid = new Blob(['%PDF-1.7\ncontenido'], { type: 'application/pdf' });
    const invalid = new Blob(['not a pdf'], { type: 'text/plain' });

    expect(await validatePdfBlob(valid, 'application/pdf')).toBeNull();
    expect(await validatePdfBlob(invalid, 'text/plain')).toContain('formato PDF');
  });

  it('uses the response filename when available', () => {
    expect(extractReportFileName('attachment; filename="reporte.pdf"', 'fallback.pdf')).toBe('reporte.pdf');
    expect(extractReportFileName(null, 'fallback.pdf')).toBe('fallback.pdf');
  });
});
