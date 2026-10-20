import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { AchColombiaFileExchangeService } from '../../core/services/ach-colombia-file-exchange.service';
import { TransferDetail, TransferFilter, TransferStatus, TransferSummary } from '../../core/models/ach-colombia-file-exchange.model';

@Component({
  selector: 'app-ach-colombia-file-exchange',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './ach-colombia-file-exchange.component.html',
  styleUrls: ['./ach-colombia-file-exchange.component.scss']
})
export class AchColombiaFileExchangeComponent implements OnInit {
  private readonly api = inject(AchColombiaFileExchangeService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  readonly canManage = this.auth.hasPermission('CanManageAch');
  rows: TransferSummary[] = [];
  selected?: TransferDetail;
  readonly statuses: TransferStatus[] = ['Ready', 'InProgress', 'Transferred', 'Received', 'Processed', 'Rejected', 'Duplicate', 'RetryPending', 'Uncertain', 'Failed', 'Retired'];
  filter: TransferFilter =
    { from: '', to: '', direction: '', status: '', executionOrigin: '', cycleId: '', fileName: '', transferId: '', archived: '', pageNumber: 1, pageSize: 50 };
  cycleId = '';
  busy = false;
  error = '';

  ngOnInit(): void { this.refresh(); }
  refresh(): void { this.run(() => this.api.list(this.filter), rows => this.rows = rows); }
  search(): void { this.filter.pageNumber = 1; this.refresh(); }
  page(delta: number): void { this.filter.pageNumber = Math.max(1, this.filter.pageNumber + delta); this.refresh(); }
  open(row: Pick<TransferSummary, 'id'>): void { this.run(() => this.api.detail(row.id), detail => this.selected = detail); }
  close(): void { this.selected = undefined; }
  executeOutbound(): void {
    if (!this.canManage || this.busy) return;
    if (!this.cycleId.trim() || !confirm('¿Deseas iniciar ahora el envío de los archivos oficiales del ciclo indicado?')) return;
    this.run(() => this.api.executeOutbound(this.cycleId.trim()), () => { this.notifications.success('La ejecución de envío terminó. Revisa el estado de las transferencias.'); this.refresh(); });
  }
  executeInbound(): void {
    if (!this.canManage || this.busy) return;
    if (!confirm('¿Deseas iniciar ahora la consulta y el procesamiento de los archivos recibidos?')) return;
    this.run(() => this.api.executeInbound(), () => { this.notifications.success('La ejecución de recepción terminó. Revisa el estado de los archivos.'); this.refresh(); });
  }
  retry(): void { if (this.canManage && !this.busy && this.selected?.canRetry && confirm('¿Deseas iniciar un reintento para este archivo?')) this.action(() => this.api.retry(this.selected!.id)); }
  reprocess(): void { if (this.canManage && !this.busy && this.selected?.canReprocess && confirm('¿Desea reprocesar este archivo con las protecciones de duplicados vigentes?')) this.action(() => this.api.reprocess(this.selected!.id)); }
  archive(): void { if (this.canManage && !this.busy && this.selected?.canArchive && confirm('¿Deseas archivar este archivo? El contenido se conservará en el archivo operativo.')) this.action(() => this.api.archive(this.selected!.id)); }
  retire(): void {
    if (!this.canManage || this.busy || !this.selected?.canRetire || !confirm('El archivo dejará de estar disponible en el área operativa, pero su historial y trazabilidad se conservarán. ¿Desea continuar?')) return;
    const reason = prompt('Indique la razón del retiro:')?.trim();
    if (reason) this.action(() => this.api.retire(this.selected!.id, reason));
  }
  download(): void {
    if (!this.selected?.contentAvailable) return;
    this.run(() => this.api.download(this.selected!.id), response => {
      const url = URL.createObjectURL(response.body!); const link = document.createElement('a');
      link.href = url; link.download = this.selected!.fileName; link.click(); URL.revokeObjectURL(url);
    });
  }
  direction(value: string): string { return value === 'Outbound' ? 'Envío' : 'Recepción'; }
  origin(value: string): string { return value === 'Automatic' ? 'Automática' : 'Manual'; }
  outcome(value: string): string { return ({ Started: 'Iniciado', Succeeded: 'Correcto', Processed: 'Procesado', Ignored: 'Ignorado', Uncertain: 'Pendiente de confirmación', RetryPending: 'Reintento pendiente', Rejected: 'Rechazado', Duplicate: 'Duplicado', Failed: 'Fallido' } as Record<string, string>)[value] ?? 'Registrado'; }
  lastSuccessfulStep(detail: TransferDetail) {
    return [...detail.history].reverse().find(x => x.result === 'Succeeded' || x.result === 'Processed');
  }
  guidance(detail: TransferDetail): string {
    if (detail.status === 'Uncertain') return detail.canRetry
      ? 'No fue posible confirmar la publicación en la frontera CFA. Puede realizar un reintento controlado.'
      : 'No fue posible confirmar la publicación en la frontera CFA. Revise el historial antes de continuar.';
    if (detail.status === 'RetryPending') return 'La operación está pendiente de reintento.';
    if (detail.canRetry) return 'Reintento disponible sobre el mismo contenido retenido.';
    if (detail.canReprocess) return 'Reproceso disponible mediante la ingestión existente.';
    if (detail.status === 'Failed' || detail.status === 'Rejected') return 'Requiere revisión operativa. Consulte el historial.';
    if (detail.status === 'Duplicate') return 'El archivo ya había sido procesado o identificado previamente; no se creó un efecto funcional adicional.';
    if (detail.status === 'Transferred') return 'Publicado en la frontera CFA. La recepción por ACH Colombia no está confirmada.';
    return '';
  }
  status(value: string): string { return ({ Ready: 'Generado', InProgress: 'En proceso', Transferred: 'Publicado para envío', Received: 'Recibido desde la frontera CFA', Processed: 'Procesado', Rejected: 'Rechazado', Duplicate: 'Duplicado', RetryPending: 'Reintento pendiente', Uncertain: 'Pendiente de confirmación', Failed: 'Fallido', Retired: 'Retirado' } as Record<string, string>)[value] ?? value; }
  statusText(item: Pick<TransferSummary, 'status' | 'statusLabel'>): string { return item.statusLabel || this.status(item.status); }
  event(value: string): string { return ({ Generated: 'Generado', Encrypted: 'Cifrado', OutboundPrepared: 'Archivo preparado', PublicationStarted: 'Publicación iniciada', PublishedToMftBoundary: 'Publicado para envío', PublicationUncertain: 'Publicación pendiente de confirmación', PublicationFailed: 'Fallido', RetryPending: 'Reintento pendiente', OutboundAttempt: 'Intento de publicación', ReceivedFromMftBoundary: 'Recibido desde la frontera CFA', InboundClaimed: 'Recibido desde la frontera CFA', InboundProcessingStarted: 'Procesamiento iniciado', InboundProcessingFinished: 'Procesamiento finalizado', Processed: 'Procesado', Rejected: 'Rechazado', Duplicate: 'Duplicado', DuplicateDetected: 'Duplicado detectado', Archived: 'Archivo archivado', Retired: 'Archivo retirado', Downloaded: 'Archivo descargado', ReprocessStarted: 'Reproceso iniciado', ReprocessFinished: 'Reproceso finalizado' } as Record<string, string>)[value] ?? value; }
  eventName(item: TransferDetail['history'][number]): string { return item.displayName || this.event(item.eventType); }
  eventResult(item: TransferDetail['history'][number]): string { return item.displayResult || this.outcome(item.result); }
  eventMessage(item: TransferDetail['history'][number]): string { return item.displayMessage || 'Evento registrado en la trazabilidad.'; }
  actor(item: TransferDetail['history'][number]): string { return item.actorDisplay || (item.executionOrigin === 'Automatic' ? 'Automatización ACH Colombia' : item.actor); }
  errorMessage(detail: TransferDetail): string {
    if (detail.status === 'Uncertain') return 'No fue posible confirmar la publicación en la frontera CFA.';
    if (detail.status === 'RetryPending') return 'La operación quedó pendiente de reintento.';
    if (detail.status === 'Failed') return detail.direction === 'Outbound' ? 'No se pudo publicar el archivo.' : 'No fue posible procesar el archivo.';
    if (detail.status === 'Rejected') return 'El archivo fue rechazado durante el procesamiento.';
    return '';
  }
  private action(request: () => ReturnType<AchColombiaFileExchangeService['retry']>): void { this.run(request, detail => { this.selected = detail; this.notifications.success('Operación completada.'); this.refresh(); }); }
  private run<T>(request: () => import('rxjs').Observable<T>, success: (value: T) => void): void {
    this.busy = true; this.error = '';
    request().pipe(finalize(() => this.busy = false)).subscribe({ next: success, error: () => {
      this.error = 'No fue posible completar la operación. Revise el estado y vuelva a intentar.';
      this.notifications.error(this.error);
    }});
  }
}
