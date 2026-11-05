import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-retire-transfer-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Retirar archivo</h2>
    <mat-dialog-content>
      <p>El archivo dejará de estar disponible en el área operativa. Su historial y trazabilidad se conservarán.</p>
      <mat-form-field appearance="outline" class="reason-field">
        <mat-label>Motivo del retiro</mat-label>
        <textarea matInput name="reason" [(ngModel)]="reason" rows="3" required
          placeholder="Explica brevemente por qué se retira" aria-required="true"></textarea>
        <mat-hint>El motivo se guardará como parte de la trazabilidad.</mat-hint>
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [mat-dialog-close]="undefined">Cancelar</button>
      <button mat-flat-button class="retire-button" type="button" [disabled]="!reason.trim()" (click)="confirm()">Retirar archivo</button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content p { line-height: 1.55; margin-top: 0; }
    .reason-field { display: block; width: 100%; margin-top: 1rem; }
    .retire-button {
      --mdc-filled-button-container-color: var(--color-error);
      --mdc-filled-button-label-text-color: var(--color-on-error);
    }
    @media(max-width:480px) { mat-dialog-actions { flex-direction: column-reverse; align-items: stretch; } mat-dialog-actions button { width: 100%; } }
  `]
})
export class RetireTransferDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<RetireTransferDialogComponent, string | undefined>);
  reason = '';
  confirm(): void { if (this.reason.trim()) this.dialogRef.close(this.reason.trim()); }
}
