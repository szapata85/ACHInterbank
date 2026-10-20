import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { map } from 'rxjs';
import { OperationalConfirmDialogComponent } from '../../../shared/components/operational-confirm-dialog.component';

export interface PendingUserChanges {
  hasPendingChanges(): boolean;
}

export const pendingUserChangesGuard: CanDeactivateFn<PendingUserChanges> = (component) => {
  if (!component.hasPendingChanges()) {
    return true;
  }

  return inject(MatDialog)
    .open(OperationalConfirmDialogComponent, {
      data: {
        title: 'Tienes cambios sin guardar',
        message: 'Si sales ahora, perderás la información diligenciada.',
        confirmLabel: 'Salir sin guardar',
        cancelLabel: 'Continuar editando',
        icon: 'edit_off'
      },
      width: 'min(92vw, 520px)',
      autoFocus: 'dialog'
    })
    .afterClosed()
    .pipe(map(Boolean));
};
