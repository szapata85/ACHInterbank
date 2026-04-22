import { sanitizeDownloadFileName } from './download-file-name.util';

describe('sanitizeDownloadFileName', () => {
  it('removes traversal and separators', () => {
    const value = sanitizeDownloadFileName('../secret/../../archivo.env', 'fallback.env');
    expect(value).toBe('secret____archivo.env');
  });

  it('forces fallback extension when incoming extension differs', () => {
    const value = sanitizeDownloadFileName('reporte.txt', 'fallback.env');
    expect(value).toBe('reporte.env');
  });

  it('uses fallback for hidden or reserved names', () => {
    expect(sanitizeDownloadFileName('.env', 'seguro.txt')).toBe('seguro.txt');
    expect(sanitizeDownloadFileName('CON', 'seguro.txt')).toBe('seguro.txt');
  });
});
