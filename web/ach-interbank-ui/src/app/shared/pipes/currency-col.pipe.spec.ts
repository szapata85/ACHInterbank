import { CurrencyColPipe } from './currency-col.pipe';

describe('CurrencyColPipe', () => {
  const pipe = new CurrencyColPipe();

  it('formatea cero, negativos y valores grandes en pesos colombianos con dos decimales', () => {
    expect(pipe.transform(0)).toContain('0,00');
    expect(pipe.transform(-1250.5)).toContain('-$');
    expect(pipe.transform(9876543210.12)).toContain('9.876.543.210,12');
  });

  it('muestra un marcador cuando no existe valor', () => {
    expect(pipe.transform(null)).toBe('-');
  });
});
