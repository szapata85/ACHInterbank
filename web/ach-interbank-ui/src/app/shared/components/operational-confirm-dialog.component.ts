import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface OperationalConfirmDialogData {
  title: string;
  message: string;
  confirmLabel: string;
  cancelLabel?: string;
  icon?: string;
}

@Component({
  selector: 'app-operational-confirm-dialog',
  standalone: true,
  imports: [MatButtonModule, MatDialogModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>
      <mat-icon aria-hidden="true">{{ data.icon || 'fact_check' }}</mat-icon>
      {{ data.title }}
    </h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [mat-dialog-close]="false">
        {{ data.cancelLabel || 'Cancelar' }}
      </button>
      <button mat-flat-button color="primary" type="button" [mat-dialog-close]="true">
        {{ data.confirmLabel }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 {
      display: flex;
      align-items: center;
      gap: .65rem;
    }

    mat-dialog-content {
      max-width: 32rem;
    }

    mat-dialog-content p {
      margin: 0;
      line-height: 1.55;
    }

    @media (max-width: 480px) {
      mat-dialog-actions {
        align-items: stretch;
        flex-direction: column-reverse;
      }

      mat-dialog-actions button {
        width: 100%;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OperationalConfirmDialogComponent {
  readonly data = inject<OperationalConfirmDialogData>(MAT_DIALOG_DATA);
}
