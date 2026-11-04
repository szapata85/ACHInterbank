import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import {
  AchColombiaReturnCandidate,
  AchColombiaReturnReviewApiService
} from '../../services/ach-colombia-return-review-api.service';

type Decision = 'APROBAR' | 'DESCARTAR' | 'GENERAR';

@Component({
  selector: 'app-ach-colombia-review-confirmation',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatDialogModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>Confirmar {{ data.action === 'APROBAR' ? 'aprobación' : data.action === 'GENERAR' ? 'generación' : 'descarte' }}</h2>
    <mat-dialog-content>
      <p>Transacción {{ data.candidate.originalTransactionId }} · Causal {{ data.candidate.reasonCode }}.</p>
      <p *ngIf="data.action === 'APROBAR'">
        Esta aprobación solo autoriza una candidata interna. No genera archivos ni envía dinero.
      </p>
      <p *ngIf="data.action === 'DESCARTAR'">
        Se conservará el registro para consulta y auditoría.
      </p>
      <p *ngIf="data.action === 'GENERAR'">
        Se creará un archivo de devolución y se descargará con el nombre oficial.
        No se enviará automáticamente a ACH Colombia ni se liquidará en la cámara.
        No podrás volver a generar la devolución de esta transacción.
      </p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="dialogRef.close(false)">Cancelar</button>
      <button mat-flat-button color="primary" type="button" (click)="dialogRef.close(true)">
        {{ data.action === 'APROBAR' ? 'Aprobar' : data.action === 'GENERAR' ? 'Generar archivo' : 'Descartar' }}
      </button>
    </mat-dialog-actions>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchColombiaReviewConfirmationComponent {
  readonly data = inject<{ candidate: AchColombiaReturnCandidate; action: Decision }>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<AchColombiaReviewConfirmationComponent>);
}

@Component({
  selector: 'app-ach-colombia-return-review',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatIconModule, MatDialogModule],
  templateUrl: './ach-colombia-return-review.component.html',
  styleUrls: ['./ach-colombia-return-review.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchColombiaReturnReviewComponent implements OnInit {
  private readonly api = inject(AchColombiaReturnReviewApiService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  candidates: AchColombiaReturnCandidate[] = [];
  loading = false;
  busyId: number | null = null;
  error: string | null = null;

  get canManage(): boolean {
    return this.auth.hasPermission(['Returns.GenerateFile', 'CanManageAch']);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    if (this.loading || this.busyId !== null) { return; }
    this.loading = true;
    this.error = null;
    this.api.list().pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => { this.loading = false; this.cdr.markForCheck(); })
    ).subscribe({
      next: (rows) => { this.candidates = Array.isArray(rows) ? rows : []; this.cdr.markForCheck(); },
      error: (error: unknown) => {
        this.candidates = [];
        this.error = error instanceof HttpErrorResponse && error.status === 403
          ? 'No tienes permiso para consultar estas candidatas.'
          : 'No se pudo consultar las candidatas. Inténtalo nuevamente.';
        this.cdr.markForCheck();
      }
    });
  }

  confirm(candidate: AchColombiaReturnCandidate, action: Decision): void {
    if (!this.canManage || this.loading || this.busyId !== null) { return; }
    if (action === 'APROBAR' && !candidate.canApprove) { return; }
    if (action === 'GENERAR' && !candidate.canGenerateFile) { return; }
    this.dialog.open(AchColombiaReviewConfirmationComponent, {
      data: { candidate, action }, disableClose: true,
      width: 'min(94vw, 480px)'
    }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((accepted: boolean | undefined) => {
      if (accepted) {
        if (action === 'GENERAR') { this.generateFile(candidate.id); }
        else { this.decide(candidate.id, action); }
      }
    });
  }

  private generateFile(id: number): void {
    if (this.busyId !== null) { return; }
    this.busyId = id;
    this.error = null;
    this.api.generateApprovedFile(id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => { this.busyId = null; this.cdr.markForCheck(); })
    ).subscribe({
      next: (file) => {
        // Se usa el nombre oficial devuelto por la política NACHA-M de ACH Colombia.
        if (!file.fileName || !file.fileName.endsWith('.RET') || !file.content) {
          this.notifications.warning('La respuesta del archivo es incompleta. Consulta su registro antes de reintentar.');
          this.busyId = null;
          this.load();
          return;
        }
        try {
          const bytes = Uint8Array.from(atob(file.content), character => character.charCodeAt(0));
          const blob = new Blob([bytes], { type: file.contentType || 'text/plain' });
          const objectUrl = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = objectUrl;
          link.download = file.fileName;
          link.click();
          URL.revokeObjectURL(objectUrl);
          this.notifications.success('Archivo de devolución generado y descargado. No fue enviado a la cámara.');
        } catch {
          this.notifications.warning('Se generó el archivo, pero no fue posible descargarlo. Consulta el registro antes de reintentar.');
        }
        this.busyId = null;
        this.load();
      },
      error: (err: unknown) => {
        this.error = err instanceof HttpErrorResponse && err.status === 409
          ? 'La candidata cambió, venció o ya tiene devolución. Actualiza la consulta.'
          : 'No fue posible generar el archivo. Consulta el estado antes de reintentar.';
        this.cdr.markForCheck();
      }
    });
  }

  recover(candidate: AchColombiaReturnCandidate): void {
    if (!this.canManage || this.busyId !== null || candidate.status !== 'ARCHIVO_GENERADO_SIN_ENVIO') { return; }
    this.busyId = candidate.id;
    this.error = null;
    this.api.recoverGeneratedFile(candidate.id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => { this.busyId = null; this.cdr.markForCheck(); })
    ).subscribe({
      next: (file) => {
        if (!file.fileName || !file.fileName.endsWith('.RET') || !file.content) {
          this.notifications.warning('El archivo recuperado es incompleto. Solicita revisión operativa.');
          return;
        }
        try {
          const bytes = Uint8Array.from(atob(file.content), character => character.charCodeAt(0));
          const blob = new Blob([bytes], { type: file.contentType || 'text/plain' });
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = file.fileName;
          link.click();
          URL.revokeObjectURL(url);
          this.notifications.success('Archivo recuperado con su nombre original. No fue enviado a la cámara.');
        } catch {
          this.notifications.warning('No fue posible descargar el archivo recuperado. Solicita revisión.');
        }
      },
      error: () => {
        this.error = 'No se pudo recuperar el archivo. No vuelvas a generarlo; solicita revisión.';
        this.cdr.markForCheck();
      }
    });
  }

  private decide(id: number, action: 'APROBAR' | 'DESCARTAR'): void {
    if (this.busyId !== null) { return; }
    this.busyId = id;
    this.error = null;
    this.api.decide(id, action).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => { this.busyId = null; this.cdr.markForCheck(); })
    ).subscribe({
      next: () => {
        this.notifications.success(action === 'APROBAR'
          ? 'Candidata aprobada para una etapa posterior. No se generó ningún archivo.'
          : 'Candidata descartada. El registro permanece disponible.');
        this.busyId = null;
        this.load();
      },
      error: (err: unknown) => {
        this.error = err instanceof HttpErrorResponse && err.status === 409
          ? 'La candidata cambió o dejó de estar habilitada. Actualiza la consulta.'
          : 'No se pudo guardar la decisión. Inténtalo nuevamente.';
        this.cdr.markForCheck();
      }
    });
  }

  transactionTypeLabel(value: string): string {
    switch (value) {
      case 'Credit': return 'Crédito';
      case 'Debit': return 'Débito';
      case 'Prenotification': return 'Prenotificación';
      default: return 'Transacción';
    }
  }

  statusLabel(value: string): string {
    switch (value) {
      case 'PREPARADA_SIN_ENVIO': return 'Pendiente de aprobación';
      case 'REVISION_TARDIA_CREDITO': return 'Crédito tardío · requiere revisión';
      case 'APROBADA_SIN_ENVIO': return 'Aprobada · sin archivo generado';
      case 'DESCARTADA': return 'Descartada';
      case 'ARCHIVO_GENERADO_SIN_ENVIO': return 'Archivo generado · sin envío';
      default: return 'Estado en revisión';
    }
  }

  trackById(_: number, row: AchColombiaReturnCandidate): number { return row.id; }
}
