import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';

export interface MappingPublicationDialogData {
  serviceName: string;
  contractVersion: string;
  mappingSetName: string;
  revision: number;
  total: number;
  complete: number;
  pendingRequired: number;
  newRequiredPending: number;
  blockingIssues: number;
  pendingRequiredNames: string[];
  canPublish: boolean;
}

@Component({
  selector: 'app-mapping-publication-dialog',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatDialogModule],
  templateUrl: './mapping-publication-dialog.component.html',
  styleUrls: ['./mapping-publication-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MappingPublicationDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) readonly data: MappingPublicationDialogData) {}

  pendingLabel(): string {
    if (this.data.pendingRequired === 1 && this.data.newRequiredPending === 1) {
      return '1 dato nuevo pendiente';
    }
    return this.data.pendingRequired === 1
      ? '1 dato requerido pendiente'
      : `${this.data.pendingRequired} datos requeridos pendientes`;
  }
}
