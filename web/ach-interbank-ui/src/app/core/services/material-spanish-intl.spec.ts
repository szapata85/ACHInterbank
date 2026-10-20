import { SpanishDatepickerIntl, SpanishPaginatorIntl } from './material-spanish-intl';

describe('Etiquetas en español de Angular Material', () => {
  it('presenta la paginación sin textos en inglés', () => {
    const intl = new SpanishPaginatorIntl();

    expect(intl.itemsPerPageLabel).toBe('Registros por página:');
    expect(intl.nextPageLabel).toBe('Página siguiente');
    expect(intl.getRangeLabel(1, 20, 45)).toBe('21 – 40 de 45');
  });

  it('presenta las acciones del calendario en español', () => {
    const intl = new SpanishDatepickerIntl();

    expect(intl.openCalendarLabel).toBe('Abrir calendario');
    expect(intl.closeCalendarLabel).toBe('Cerrar calendario');
    expect(intl.switchToMultiYearViewLabel).toBe('Elegir mes y año');
  });
});
