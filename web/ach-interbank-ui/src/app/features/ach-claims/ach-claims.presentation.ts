import {
  AchTransactionType,
  ClaimCaseEventType,
  ClaimCaseStatus,
  ClaimCauseCode
} from './ach-claims.models';

export function claimStatusLabel(status: ClaimCaseStatus): string {
  const labels: Record<ClaimCaseStatus, string> = {
    [ClaimCaseStatus.Open]: 'Abierto',
    [ClaimCaseStatus.Resolved]: 'Resuelto',
    [ClaimCaseStatus.Expired]: 'Cerrado por vencimiento'
  };
  return labels[status] ?? 'Estado no disponible';
}

export function claimCauseLabel(cause: ClaimCauseCode, compact = false): string {
  if (cause === ClaimCauseCode.Dev14) {
    return compact ? 'Débito ACH no consentido' : 'DEV14 — Débito ACH no consentido';
  }
  return 'Causal no disponible';
}

export function claimEventLabel(eventType: ClaimCaseEventType): string {
  const labels: Record<ClaimCaseEventType, string> = {
    [ClaimCaseEventType.Opened]: 'Apertura',
    [ClaimCaseEventType.Progress]: 'Avance',
    [ClaimCaseEventType.Resolved]: 'Resolución',
    [ClaimCaseEventType.Expired]: 'Cierre por vencimiento'
  };
  return labels[eventType] ?? 'Movimiento';
}

export function transactionTypeLabel(type: AchTransactionType): string {
  const labels: Record<AchTransactionType, string> = {
    [AchTransactionType.Credit]: 'Crédito',
    [AchTransactionType.Debit]: 'Débito',
    [AchTransactionType.Prenotification]: 'Prenotificación',
    [AchTransactionType.Reversal]: 'Reverso',
    [AchTransactionType.Return]: 'Devolución'
  };
  return labels[type] ?? 'Tipo no disponible';
}
