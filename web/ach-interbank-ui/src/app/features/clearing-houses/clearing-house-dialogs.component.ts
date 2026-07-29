import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';

export interface ConfirmationDialogData {
  title: string;
  message: string;
  confirmText: string;
  icon?: string;
  destructive?: boolean;
}

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatDialogModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>
      <mat-icon aria-hidden="true">{{ data.icon || 'help_outline' }}</mat-icon>
      {{ data.title }}
    </h2>
    <mat-dialog-content><p>{{ data.message }}</p></mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [mat-dialog-close]="false">Cancelar</button>
      <button
        mat-flat-button
        type="button"
        [color]="data.destructive ? 'warn' : 'primary'"
        [mat-dialog-close]="true"
      >
        {{ data.confirmText }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 { display: flex; align-items: center; gap: .625rem; }
    p { color: #475569; line-height: 1.55; max-width: 34rem; }
  `]
})
export class ConfirmationDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) readonly data: ConfirmationDialogData) {}
}

export interface CloseValidityDialogData {
  transactionType: string;
  initialDate: Date;
}

@Component({
  selector: 'app-close-validity-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatNativeDateModule
  ],
  template: `
    <h2 mat-dialog-title><mat-icon aria-hidden="true">event_busy</mat-icon> Cerrar vigencia</h2>
    <mat-dialog-content>
      <p>Defina la fecha final para la política de {{ data.transactionType }}. El historial se conservará.</p>
      <mat-form-field appearance="outline">
        <mat-label>Vigente hasta</mat-label>
        <input matInput [matDatepicker]="picker" [formControl]="date" />
        <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
        <mat-datepicker #picker></mat-datepicker>
        <mat-error *ngIf="date.hasError('required')">La fecha final es obligatoria.</mat-error>
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button color="primary" type="button" [disabled]="date.invalid" (click)="submit()">
        Cerrar vigencia
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 { display: flex; align-items: center; gap: .625rem; }
    p { color: #475569; line-height: 1.55; max-width: 34rem; }
    mat-form-field { width: 100%; margin-top: .75rem; }
  `]
})
export class CloseValidityDialogComponent {
  readonly date: FormControl<Date | null>;

  constructor(
    @Inject(MAT_DIALOG_DATA) readonly data: CloseValidityDialogData,
    readonly dialogRef: MatDialogRef<CloseValidityDialogComponent, Date | undefined>
  ) {
    this.date = new FormControl<Date | null>(data.initialDate, Validators.required);
  }

  submit(): void {
    if (this.date.valid && this.date.value) {
      this.dialogRef.close(this.date.value);
    }
  }
}
