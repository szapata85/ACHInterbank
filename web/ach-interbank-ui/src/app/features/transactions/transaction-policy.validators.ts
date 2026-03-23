import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { AccountTypeEnum, TransactionTypeEnum } from './transactions.types';
import { TransactionPolicyPreview } from './transactions.models';

export function recipientIdentityValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const personType = String(control.get('recipientPersonType')?.value ?? '').trim().toUpperCase();
    const value = String(control.get('recipientIdNumber')?.value ?? '').trim().toUpperCase();

    if (!value) {
      return null;
    }

    if (personType === 'PN' && !/^\d{5,20}$/.test(value)) {
      return { recipientIdentityFormat: true };
    }

    if (personType === 'PJ' && !/^[A-Z0-9]{4,20}$/.test(value)) {
      return { recipientIdentityFormat: true };
    }

    return null;
  };
}

export function policyPreviewValidator(getPreview: () => TransactionPolicyPreview | null): ValidatorFn {
  return (_control: AbstractControl): ValidationErrors | null => {
    const preview = getPreview();
    if (!preview) {
      return null;
    }

    return preview.canSubmit ? null : { policyRejected: preview.message ?? 'La transacción incumple la política ACH.' };
  };
}

export function allowedAccountTypesForTransaction(type: TransactionTypeEnum): AccountTypeEnum[] {
  return type === TransactionTypeEnum.Debit
    ? [AccountTypeEnum.Checking, AccountTypeEnum.Savings, AccountTypeEnum.ElectronicDeposits]
    : [AccountTypeEnum.Checking, AccountTypeEnum.Savings, AccountTypeEnum.ElectronicDeposits];
}
