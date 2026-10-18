import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchClaimsApiService } from '../ach-claims-api.service';
import { claimsErrorMessage, newIdempotencyKey } from '../ach-claims-error.util';
import { ClaimCaseDetail, CreateDev14ClaimCaseRequest } from '../ach-claims.models';

@Component({
  selector: 'app-create-dev14-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatIconModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Nueva reclamación por débito no consentido</h2>
    <mat-dialog-content>
      <p class="dialog-intro">Asocia una o varias transacciones débito de ACH Colombia. Las entidades y la fecha límite se determinan automáticamente.</p>
      <form [formGroup]="form" (ngSubmit)="submit()" id="create-dev14-form">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Transacciones asociadas</mat-label>
          <textarea matInput rows="4" formControlName="transactionIdsText"
            placeholder="Ejemplo: 12345, 12346 12347"></textarea>
          <mat-hint>Ingresa los números de las transacciones que deseas incluir, separados por coma o espacio.</mat-hint>
          <mat-error *ngIf="form.controls.transactionIdsText.hasError('required')">Ingresa al menos una transacción.</mat-error>
          <mat-error *ngIf="form.controls.transactionIdsText.hasError('invalidIds')">Usa únicamente números enteros positivos separados por coma o espacio.</mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Referencia externa (opcional)</mat-label>
          <input matInput formControlName="externalReference" maxlength="200" />
          <mat-hint>Úsala solo si cuentas con una referencia operativa externa.</mat-hint>
        </mat-form-field>

        <div *ngIf="error()" class="form-error" role="alert">{{ error() }}</div>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [disabled]="saving()" (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button type="submit" form="create-dev14-form" [disabled]="saving()">
        <mat-icon *ngIf="!saving()">add_circle</mat-icon>{{ saving() ? 'Creando…' : 'Crear reclamación' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .dialog-intro { max-width: 38rem; color: var(--mat-sys-on-surface-variant); }
    .full-width { display: block; width: 100%; margin-top: 1rem; }
    .form-error { margin-top: .75rem; color: var(--mat-sys-error); }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreateDev14DialogComponent {
  readonly dialogRef = inject(MatDialogRef<CreateDev14DialogComponent, ClaimCaseDetail | undefined>);
  private readonly api = inject(AchClaimsApiService);
  private readonly notifications = inject(NotificationService);

  readonly saving = signal(false);
  readonly error = signal('');
  private lastPayload = '';
  private idempotencyKey = '';

  readonly form = new FormGroup({
    transactionIdsText: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    externalReference: new FormControl('', { nonNullable: true })
  });

  submit(): void {
    const transactionIds = this.parseTransactionIds(this.form.controls.transactionIdsText.value);
    if (transactionIds.length === 0) {
      this.form.controls.transactionIdsText.setErrors({ ...(this.form.controls.transactionIdsText.errors ?? {}), required: true });
      this.form.controls.transactionIdsText.markAsTouched();
      return;
    }
    if (!this.validTransactionText(this.form.controls.transactionIdsText.value)) {
      this.form.controls.transactionIdsText.setErrors({ invalidIds: true });
      this.form.controls.transactionIdsText.markAsTouched();
      return;
    }

    this.form.controls.transactionIdsText.setErrors(null);
    const externalReference = this.form.controls.externalReference.value.trim();
    const request: CreateDev14ClaimCaseRequest = {
      transactionIds,
      ...(externalReference ? { externalReference } : {})
    };
    const payload = JSON.stringify(request);
    if (payload !== this.lastPayload) {
      this.lastPayload = payload;
      this.idempotencyKey = newIdempotencyKey();
    }

    this.saving.set(true);
    this.error.set('');
    this.api.createDev14(request, this.idempotencyKey).pipe(
      finalize(() => this.saving.set(false))
    ).subscribe({
      next: created => {
        this.notifications.success('La reclamación fue creada correctamente.');
        this.dialogRef.close(created);
      },
      error: err => this.error.set(claimsErrorMessage(
        err,
        'No fue posible crear la reclamación porque una de las transacciones no cumple las condiciones requeridas.'))
    });
  }

  parseTransactionIds(value: string): number[] {
    const tokens = value.trim().split(/[\s,;]+/).filter(Boolean);
    const ids = tokens.map(token => Number(token)).filter(value => Number.isInteger(value) && value > 0);
    return [...new Set(ids)];
  }

  private validTransactionText(value: string): boolean {
    const tokens = value.trim().split(/[\s,;]+/).filter(Boolean);
    return tokens.length > 0 && tokens.every(token => /^\d+$/.test(token) && Number(token) > 0);
  }
}
