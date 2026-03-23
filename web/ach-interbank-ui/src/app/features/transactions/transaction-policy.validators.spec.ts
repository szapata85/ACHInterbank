import { FormControl, FormGroup } from '@angular/forms';
import { policyPreviewValidator, recipientIdentityValidator } from './transaction-policy.validators';

describe('transaction-policy validators', () => {
  it('rejects natural-person ids with alphanumeric content', () => {
    const form = new FormGroup({
      recipientPersonType: new FormControl('PN'),
      recipientIdNumber: new FormControl('ABCD123')
    }, { validators: recipientIdentityValidator() });

    expect(form.errors).toEqual({ recipientIdentityFormat: true });
  });

  it('rejects forms when policy preview marks the transaction as invalid', () => {
    const form = new FormGroup({
      amount: new FormControl(1000)
    }, { validators: policyPreviewValidator(() => ({
      canSubmit: false,
      isWithinProcessingWindow: false,
      wouldDuplicate: false,
      message: 'Fuera de horario'
    })) });

    expect(form.errors).toEqual({ policyRejected: 'Fuera de horario' });
  });
});
