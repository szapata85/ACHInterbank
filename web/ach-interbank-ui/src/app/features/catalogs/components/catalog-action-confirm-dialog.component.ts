import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

export interface CatalogActionConfirmDialogData {
  title: string;
  message: string;
  confirmText: string;
  destructive?: boolean;
}

@Component({
  selector: 'app-catalog-action-confirm-dialog',
  standalone: true,
  imports: [MatButtonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="close(false)">Cancelar</button>
      <button
        mat-flat-button
        type="button"
        [class.catalog-confirm-dialog__danger]="data.destructive"
        (click)="close(true)"
      >
        {{ data.confirmText }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      mat-dialog-content {
        max-width: 32rem;
      }

      mat-dialog-content p {
        line-height: 1.55;
        margin: 0;
        overflow-wrap: anywhere;
      }

      .catalog-confirm-dialog__danger {
        background: var(--color-danger, #b42318);
        color: #fff;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CatalogActionConfirmDialogComponent {
  readonly data = inject<CatalogActionConfirmDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<CatalogActionConfirmDialogComponent>);

  close(confirmed: boolean): void {
    this.dialogRef.close(confirmed);
  }
}
