import { CommonModule } from '@angular/common';
import { Component, HostListener, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { Observable, finalize, take } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { AchColombiaFileExchangeService } from '../../core/services/ach-colombia-file-exchange.service';
import { TransferDetail, TransferFilter, TransferStatus, TransferSummary } from '../../core/models/ach-colombia-file-exchange.model';
import { OperationalConfirmDialogComponent, OperationalConfirmDialogData } from '../../shared/components/operational-confirm-dialog.component';
import { RetireTransferDialogComponent } from './retire-transfer-dialog.component';

const initialFilter = (): TransferFilter => ({
  from: '', to: '', direction: '', status: '', executionOrigin: '', cycleId: '',
  fileName: '', transferId: '', archived: '', pageNumber: 1, pageSize: 50
});

@Component({
  selector: 'app-ach-colombia-file-exchange',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink, MatButtonModule, MatCardModule,
    MatDialogModule, MatExpansionModule, MatFormFieldModule, MatIconModule,
    MatInputModule, MatProgressBarModule, MatSelectModule
  ],
  templateUrl: './ach-colombia-file-exchange.component.html',
  styleUrls: ['./ach-colombia-file-exchange.component.scss']
})
export class AchColombiaFileExchangeComponent implements OnInit {
  private readonly api = inject(AchColombiaFileExchangeService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);

  readonly canManage = this.auth.hasPermission('CanManageAch');
  readonly statuses: TransferStatus[] = [
    'Ready', 'InProgress', 'Transferred', 'Received', 'Processed', 'Rejected',
    'Duplicate', 'RetryPending', 'Uncertain', 'Failed', 'Retired'
  ];
  readonly pageSize = 50;
  rows: TransferSummary[] = [];
  selected?: TransferDetail;
  filter: TransferFilter = initialFilter();
  cycleId = '';
  manualCycleError = '';
  listBusy = false;
  detailBusy = false;
  operationBusy = false;
  downloadBusy = false;
  detailVisible = false;
  listError = '';
  detailError = '';
  error = '';
  openedId = '';

  get busy(): boolean { return this.listBusy || this.detailBusy || this.operationBusy || this.downloadBusy; }
  get invalidDateRange(): boolean { return !!this.filter.from && !!this.filter.to && this.filter.from > this.filter.to; }
  get hasNextPage(): boolean { return this.rows.length === this.filter.pageSize; }

  ngOnInit(): void { this.refresh(); }

  refresh(): void {
    if (this.listBusy) return;
    this.listBusy = true;
    this.listError = '';
    this.api.list({ ...this.filter }).pipe(finalize(() => this.listBusy = false)).subscribe({
      next: rows => this.rows = rows ?? [],
      error: () => {
        this.rows = [];
        this.listError = 'No pudimos consultar los archivos. Comprueba la conexión e inténtalo de nuevo.';
        this.notifications.error(this.listError);
      }
    });
  }

  search(): void {
    if (this.listBusy || this.invalidDateRange) return;
    this.filter.pageNumber = 1;
    this.refresh();
  }

  clearFilters(): void {
    if (this.listBusy) return;
    this.filter = initialFilter();
    this.refresh();
  }

  page(delta: number): void {
    if (this.listBusy || delta > 0 && !this.hasNextPage || delta < 0 && this.filter.pageNumber === 1) return;
    this.filter.pageNumber = Math.max(1, this.filter.pageNumber + delta);
    this.refresh();
  }

  open(row: Pick<TransferSummary, 'id'>): void {
    if (this.detailBusy) return;
    this.openedId = row.id;
    this.selected = undefined;
    this.detailVisible = true;
    this.detailError = '';
    this.detailBusy = true;
    this.api.detail(row.id).pipe(finalize(() => this.detailBusy = false)).subscribe({
      next: detail => { if (this.openedId === row.id) this.selected = detail; },
      error: () => {
        if (this.openedId === row.id) this.detailError = 'No fue posible abrir el archivo. Puedes volver a intentarlo.';
      }
    });
  }

  close(): void {
    this.openedId = '';
    this.detailVisible = false;
    this.selected = undefined;
    this.detailError = '';
  }

  @HostListener('document:keydown.escape')
  closeOnEscape(): void { if (this.detailVisible && !this.dialog.openDialogs?.length) this.close(); }

  executeOutbound(): void {
    if (!this.canManage || this.busy) return;
    const cycle = this.cycleId.trim();
    this.manualCycleError = cycle ? '' : 'Escribe el identificador del ciclo que deseas enviar.';
    if (!cycle) return;
    this.confirm({
      title: 'Confirmar envío del ciclo',
      message: `Se iniciará el envío de los archivos del ciclo ${cycle}. Esta acción puede generar una transferencia real.`,
      confirmLabel: 'Iniciar envío', icon: 'outbox'
    }, () => this.execute(() => this.api.executeOutbound(cycle), 'El envío terminó. Consulta el estado de los archivos.'));
  }

  executeInbound(): void {
    if (!this.canManage || this.busy) return;
    this.confirm({
      title: 'Consultar y procesar archivos recibidos',
      message: 'Se iniciará la recepción y el procesamiento de los archivos disponibles. Podrás revisar el resultado en esta bandeja.',
      confirmLabel: 'Iniciar recepción', icon: 'move_to_inbox'
    }, () => this.execute(() => this.api.executeInbound(), 'La recepción terminó. Consulta el estado de los archivos.'));
  }

  retry(): void {
    const item = this.selected;
    if (!this.canManage || this.busy || !item?.canRetry) return;
    this.confirm({ title: 'Reintentar transferencia', message: 'Se intentará transferir nuevamente el contenido retenido, con los controles de duplicados existentes.', confirmLabel: 'Reintentar' },
      () => this.action(() => this.api.retry(item.id), 'Se solicitó el reintento.'));
  }

  reprocess(): void {
    const item = this.selected;
    if (!this.canManage || this.busy || !item?.canReprocess) return;
    this.confirm({ title: 'Reprocesar archivo', message: 'Se volverá a procesar el archivo con los controles de duplicados establecidos. Revisa su historial antes de continuar.', confirmLabel: 'Reprocesar' },
      () => this.action(() => this.api.reprocess(item.id), 'Se solicitó el reproceso.'));
  }

  archive(): void {
    const item = this.selected;
    if (!this.canManage || this.busy || !item?.canArchive) return;
    this.confirm({ title: 'Archivar archivo', message: 'El archivo pasará al archivo operativo. Su contenido y trazabilidad se conservarán según las reglas vigentes.', confirmLabel: 'Archivar' },
      () => this.action(() => this.api.archive(item.id), 'Archivo archivado.'));
  }

  retire(): void {
    const item = this.selected;
    if (!this.canManage || this.busy || !item?.canRetire) return;
    this.dialog.open<RetireTransferDialogComponent, void, string | undefined>(RetireTransferDialogComponent, {
      width: 'min(520px, calc(100vw - 2rem))', autoFocus: 'first-tabbable'
    }).afterClosed().pipe(take(1)).subscribe(reason => {
      if (typeof reason === 'string' && reason.trim() && !this.busy && this.selected?.id === item.id) {
        this.action(() => this.api.retire(item.id, reason.trim()), 'Archivo retirado. Su historial permanece disponible.');
      }
    });
  }

  download(): void {
    const item = this.selected;
    if (this.downloadBusy || !item?.contentAvailable) return;
    this.downloadBusy = true;
    this.error = '';
    this.api.download(item.id).pipe(finalize(() => this.downloadBusy = false)).subscribe({
      next: response => {
        if (!response.body) {
          this.showError('El contenido no está disponible para descargar.');
          return;
        }
        const url = URL.createObjectURL(response.body);
        const link = document.createElement('a');
        link.href = url;
        link.download = item.fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
      },
      error: () => this.showError('No fue posible descargar el archivo. Inténtalo nuevamente.')
    });
  }

  trackByTransferId(_index: number, row: TransferSummary): string { return row.id; }
  direction(value: string): string { return value === 'Outbound' ? 'Envío' : value === 'Inbound' ? 'Recepción' : 'No especificada'; }
  origin(value: string): string { return value === 'Automatic' ? 'Automática' : value === 'Manual' ? 'Manual' : 'No especificada'; }
  outcome(value: string): string {
    return ({ Started: 'Iniciado', Succeeded: 'Correcto', Processed: 'Procesado', Ignored: 'Ignorado', Uncertain: 'Pendiente de confirmación', RetryPending: 'Reintento pendiente', Rejected: 'Rechazado', Duplicate: 'Duplicado', Failed: 'Fallido' } as Record<string, string>)[value] ?? 'Registrado';
  }
  lastSuccessfulStep(detail: TransferDetail) { return [...detail.history].reverse().find(x => x.result === 'Succeeded' || x.result === 'Processed'); }
  guidance(detail: TransferDetail): string {
    if (detail.status === 'Uncertain') return detail.canRetry
      ? 'No se confirmó la publicación en el punto de intercambio (CFA). Puedes realizar un reintento controlado.'
      : 'No se confirmó la publicación en el punto de intercambio (CFA). Revisa el historial antes de continuar.';
    if (detail.status === 'RetryPending') return 'La operación está pendiente de reintento.';
    if (detail.canRetry) return 'Puedes reintentar la transferencia con el contenido retenido.';
    if (detail.canReprocess) return 'El archivo está disponible para reproceso controlado.';
    if (detail.status === 'Failed' || detail.status === 'Rejected') return 'El archivo requiere revisión. Consulta el historial para conocer los pasos realizados.';
    if (detail.status === 'Duplicate') return 'Este archivo ya había sido identificado; no se creó un efecto funcional adicional.';
    if (detail.status === 'Transferred') return 'El archivo se publicó para envío. ACH Colombia aún no ha confirmado su recepción.';
    return '';
  }
  status(value: string): string {
    return ({ Ready: 'Generado', InProgress: 'En proceso', Transferred: 'Publicado para envío', Received: 'Recibido desde el punto de intercambio', Processed: 'Procesado', Rejected: 'Rechazado', Duplicate: 'Duplicado', RetryPending: 'Reintento pendiente', Uncertain: 'Pendiente de confirmación', Failed: 'Fallido', Retired: 'Retirado' } as Record<string, string>)[value] ?? 'Estado no identificado';
  }
  statusText(item: Pick<TransferSummary, 'status' | 'statusLabel'>): string { return item.statusLabel || this.status(item.status); }
  statusTone(status: TransferStatus): 'good' | 'warning' | 'bad' | 'neutral' {
    if (status === 'Processed' || status === 'Received') return 'good';
    if (status === 'Failed' || status === 'Rejected') return 'bad';
    if (status === 'Uncertain' || status === 'RetryPending' || status === 'Duplicate') return 'warning';
    return 'neutral';
  }
  event(value: string): string {
    return ({ Generated: 'Generado', Encrypted: 'Cifrado', OutboundPrepared: 'Archivo preparado', PublicationStarted: 'Publicación iniciada', PublishedToMftBoundary: 'Publicado para envío', PublicationUncertain: 'Publicación pendiente de confirmación', PublicationFailed: 'Fallido', RetryPending: 'Reintento pendiente', OutboundAttempt: 'Intento de publicación', ReceivedFromMftBoundary: 'Recibido desde el punto de intercambio', InboundClaimed: 'Recibido desde el punto de intercambio', InboundProcessingStarted: 'Procesamiento iniciado', InboundProcessingFinished: 'Procesamiento finalizado', Processed: 'Procesado', Rejected: 'Rechazado', Duplicate: 'Duplicado', DuplicateDetected: 'Duplicado detectado', Archived: 'Archivo archivado', Retired: 'Archivo retirado', Downloaded: 'Archivo descargado', ReprocessStarted: 'Reproceso iniciado', ReprocessFinished: 'Reproceso finalizado' } as Record<string, string>)[value] ?? 'Movimiento registrado';
  }
  eventName(item: TransferDetail['history'][number]): string { return item.displayName || this.event(item.eventType); }
  eventResult(item: TransferDetail['history'][number]): string { return item.displayResult || this.outcome(item.result); }
  eventMessage(item: TransferDetail['history'][number]): string { return item.displayMessage || 'Movimiento registrado en el historial.'; }
  actor(item: TransferDetail['history'][number]): string { return item.actorDisplay || (item.executionOrigin === 'Automatic' ? 'Automatización ACH Colombia' : 'Usuario autorizado'); }
  errorMessage(detail: TransferDetail): string {
    if (detail.status === 'Uncertain') return 'No fue posible confirmar la publicación para envío.';
    if (detail.status === 'RetryPending') return 'La operación quedó pendiente de reintento.';
    if (detail.status === 'Failed') return detail.direction === 'Outbound' ? 'No se pudo publicar el archivo.' : 'No fue posible procesar el archivo.';
    if (detail.status === 'Rejected') return 'El archivo fue rechazado durante el procesamiento.';
    return '';
  }

  private confirm(data: OperationalConfirmDialogData, accepted: () => void): void {
    this.dialog.open<OperationalConfirmDialogComponent, OperationalConfirmDialogData, boolean>(
      OperationalConfirmDialogComponent, { data, width: 'min(480px, calc(100vw - 2rem))' }
    ).afterClosed().pipe(take(1)).subscribe(result => { if (result === true && !this.busy) accepted(); });
  }

  private action(request: () => Observable<TransferDetail>, successMessage: string): void {
    this.execute(request, successMessage, detail => {
      if (this.selected?.id === detail.id) this.selected = detail;
    });
  }

  private execute<T>(request: () => Observable<T>, successMessage: string, onSuccess?: (value: T) => void): void {
    if (this.operationBusy) return;
    this.operationBusy = true;
    this.error = '';
    request().pipe(finalize(() => this.operationBusy = false)).subscribe({
      next: result => {
        onSuccess?.(result);
        this.notifications.success(successMessage);
        // La actualización de la lista usa su propio estado para evitar carreras entre operaciones.
        this.refresh();
      },
      error: () => this.showError('No fue posible completar la operación. Revisa el estado y vuelve a intentar.')
    });
  }

  private showError(message: string): void {
    this.error = message;
    this.notifications.error(message);
  }
}
