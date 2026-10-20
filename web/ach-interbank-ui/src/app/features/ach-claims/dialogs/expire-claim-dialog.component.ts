import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchClaimsApiService } from '../ach-claims-api.service';
import { claimsErrorMessage, newIdempotencyKey } from '../ach-claims-error.util';
import { ClaimCaseDetail } from '../ach-claims.models';

@Component({
  selector: 'app-expire-claim-dialog',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>Cerrar por vencimiento</h2>
    <mat-dialog-content>
      <p><strong>¿Deseas cerrar esta reclamación por vencimiento?</strong></p>
      <p>El sistema verificará la fecha límite de gestión antes de realizar el cierre.</p>
      <div *ngIf="error()" class="form-error" role="alert">{{ error() }}</div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [disabled]="saving()" (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button type="button" [disabled]="saving()" (click)="submit()">{{ saving() ? 'Verificando…' : 'Cerrar por vencimiento' }}</button>
    </mat-dialog-actions>
  `,
  styles: [`.form-error{margin-top:1rem;color:var(--mat-sys-error)}`],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ExpireClaimDialogComponent {
  readonly dialogRef = inject(MatDialogRef<ExpireClaimDialogComponent, ClaimCaseDetail | undefined>);
  private readonly api = inject(AchClaimsApiService);
  private readonly notifications = inject(NotificationService);
  readonly saving = signal(false);
  readonly error = signal('');
  private readonly idempotencyKey = newIdempotencyKey();

  constructor(@Inject(MAT_DIALOG_DATA) readonly data: { id: number }) {}

  submit(): void {
    this.saving.set(true);
    this.error.set('');
    this.api.expire(this.data.id, {}, this.idempotencyKey).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: detail => {
        this.notifications.success('La reclamación fue cerrada por vencimiento.');
        this.dialogRef.close(detail);
      },
      error: err => this.error.set(claimsErrorMessage(err, 'No fue posible cerrar la reclamación por vencimiento.'))
    });
  }
}
