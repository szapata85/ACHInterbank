import { catalogErrorMessage } from './catalog-error-message.util';

describe('catalogErrorMessage', () => {
  const fallback = 'No fue posible completar la operación.';

  it('keeps a short business detail', () => {
    expect(
      catalogErrorMessage({ error: { detail: 'La relación ya existe' } }, fallback)
    ).toBe('La relación ya existe');
  });

  it('rejects stack traces and database details', () => {
    expect(
      catalogErrorMessage(
        { error: { detail: 'System.InvalidOperationException at Service.Save() SQLSTATE 23505' } },
        fallback
      )
    ).toBe(fallback);
  });

  it('rejects secrets and tokens', () => {
    expect(
      catalogErrorMessage({ error: { detail: 'Authorization: Bearer eyJabcdefghi.payload' } }, fallback)
    ).toBe(fallback);
  });

  it('rejects raw object rendering', () => {
    expect(catalogErrorMessage({ message: '[object Object]' }, fallback)).toBe(fallback);
  });
});
