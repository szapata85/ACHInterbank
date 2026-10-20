import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchClaimsApiService } from '../ach-claims-api.service';
import { claimsErrorMessage, newIdempotencyKey } from '../ach-claims-error.util';
import { ClaimCaseDetail, ResolveClaimCaseRequest } from '../ach-claims.models';

@Component({
  selector: 'app-resolve-claim-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Resolver reclamación</h2>
    <mat-dialog-content>
      <p><strong>¿Deseas marcar esta reclamación como resuelta?</strong></p>
      <p>Una vez resuelta no podrán registrarse nuevos avances.</p>
      <form [formGroup]="form" (ngSubmit)="submit()" id="resolve-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Resultado de la reclamación</mat-label>
          <textarea matInput rows="5" formControlName="resolutionNotes" maxlength="2000"></textarea>
          <mat-hint>Describe brevemente el resultado de la gestión.</mat-hint>
        </mat-form-field>
        <div *ngIf="error()" class="form-error" role="alert">{{ error() }}</div>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [disabled]="saving()" (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button type="submit" form="resolve-form" [disabled]="saving()">{{ saving() ? 'Guardando…' : 'Marcar como resuelta' }}</button>
    </mat-dialog-actions>
  `,
  styles: [`.full-width{display:block;width:100%;min-width:min(32rem,75vw);margin-top:1rem} .form-error{color:var(--mat-sys-error)}`],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ResolveClaimDialogComponent {
  readonly dialogRef = inject(MatDialogRef<ResolveClaimDialogComponent, ClaimCaseDetail | undefined>);
  private readonly api = inject(AchClaimsApiService);
  private readonly notifications = inject(NotificationService);
  readonly saving = signal(false);
  readonly error = signal('');
  private lastPayload = '';
  private idempotencyKey = '';
  readonly form = new FormGroup({ resolutionNotes: new FormControl('', { nonNullable: true }) });

  constructor(@Inject(MAT_DIALOG_DATA) readonly data: { id: number }) {}

  submit(): void {
    const resolutionNotes = this.form.controls.resolutionNotes.value.trim();
    const request: ResolveClaimCaseRequest = resolutionNotes ? { resolutionNotes } : {};
    const payload = JSON.stringify(request);
    if (payload !== this.lastPayload) {
      this.lastPayload = payload;
      this.idempotencyKey = newIdempotencyKey();
    }
    this.saving.set(true);
    this.error.set('');
    this.api.resolve(this.data.id, request, this.idempotencyKey).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: detail => {
        this.notifications.success('La reclamación fue marcada como resuelta.');
        this.dialogRef.close(detail);
      },
      error: err => this.error.set(claimsErrorMessage(err, 'No fue posible resolver la reclamación.'))
    });
  }
}
