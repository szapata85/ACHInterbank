import { calculateAchRate, formatAchBoolean, formatAchDate, formatAchValue, normalizeAchFilter } from './ach-response-formatters';

describe('ach-response-formatters', () => {
  it('formats values', () => {
    expect(formatAchValue('')).toBe('-');
    expect(formatAchValue(true)).toBe('Sí');
  });

  it('formats booleans', () => {
    expect(formatAchBoolean(true)).toBe('Sí');
    expect(formatAchBoolean(null)).toBe('-');
  });

  it('formats date and normalize filters', () => {
    expect(formatAchDate('invalid')).toBe('invalid');
    expect(normalizeAchFilter('  abc ')).toBe('abc');
  });

  it('calculates rates', () => {
    expect(calculateAchRate(5, 10)).toBe('50%');
    expect(calculateAchRate(1, 3)).toBe('33.3%');
  });
});
