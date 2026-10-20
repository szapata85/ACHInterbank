import { ClaimCaseStatus } from './ach-claims.models';
import { claimStatusLabel } from './ach-claims.presentation';

describe('presentación de estados de reclamaciones', () => {
  it('no expone los nombres técnicos Open, Resolved o Expired', () => {
    const labels = [
      claimStatusLabel(ClaimCaseStatus.Open),
      claimStatusLabel(ClaimCaseStatus.Resolved),
      claimStatusLabel(ClaimCaseStatus.Expired)
    ];

    expect(labels).toEqual(['Abierto', 'Resuelto', 'Cerrado por vencimiento']);
    expect(labels.join(' ')).not.toMatch(/\bOpen\b|\bResolved\b|\bExpired\b/);
  });
});
