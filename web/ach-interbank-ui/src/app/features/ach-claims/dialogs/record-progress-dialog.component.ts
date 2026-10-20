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
import { ClaimCaseDetail, RecordClaimProgressRequest } from '../ach-claims.models';

@Component({
  selector: 'app-record-progress-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Registrar avance</h2>
    <mat-dialog-content>
      <p>Registra información relevante sobre la gestión realizada para esta reclamación.</p>
      <form [formGroup]="form" (ngSubmit)="submit()" id="progress-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Observación</mat-label>
          <textarea matInput rows="5" formControlName="observation" maxlength="2000"></textarea>
        </mat-form-field>
        <div *ngIf="error()" class="form-error" role="alert">{{ error() }}</div>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [disabled]="saving()" (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button type="submit" form="progress-form" [disabled]="saving()">{{ saving() ? 'Guardando…' : 'Registrar avance' }}</button>
    </mat-dialog-actions>
  `,
  styles: [`.full-width{display:block;width:100%;min-width:min(32rem,75vw)} .form-error{color:var(--mat-sys-error)}`],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RecordProgressDialogComponent {
  readonly dialogRef = inject(MatDialogRef<RecordProgressDialogComponent, ClaimCaseDetail | undefined>);
  private readonly api = inject(AchClaimsApiService);
  private readonly notifications = inject(NotificationService);
  readonly saving = signal(false);
  readonly error = signal('');
  private lastPayload = '';
  private idempotencyKey = '';
  readonly form = new FormGroup({ observation: new FormControl('', { nonNullable: true }) });

  constructor(@Inject(MAT_DIALOG_DATA) readonly data: { id: number }) {}

  submit(): void {
    const observation = this.form.controls.observation.value.trim();
    const request: RecordClaimProgressRequest = observation ? { observation } : {};
    const payload = JSON.stringify(request);
    if (payload !== this.lastPayload) {
      this.lastPayload = payload;
      this.idempotencyKey = newIdempotencyKey();
    }
    this.saving.set(true);
    this.error.set('');
    this.api.recordProgress(this.data.id, request, this.idempotencyKey).pipe(
      finalize(() => this.saving.set(false))
    ).subscribe({
      next: detail => {
        this.notifications.success('El avance fue registrado correctamente.');
        this.dialogRef.close(detail);
      },
      error: err => this.error.set(claimsErrorMessage(err, 'No fue posible registrar el avance.'))
    });
  }
}
