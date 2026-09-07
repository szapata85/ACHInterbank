import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { AchColombiaFileExchangeService } from './ach-colombia-file-exchange.service';
import { TransferDetail, TransferFilter, TransferStatus, TransferSummary } from './ach-colombia-file-exchange.models';

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
    if (!this.cycleId.trim() || !confirm('¿Desea enviar ahora los archivos oficiales del ciclo indicado?')) return;
    this.run(() => this.api.executeOutbound(this.cycleId.trim()), () => { this.notifications.success('La solicitud de envío finalizó.'); this.refresh(); });
  }
  executeInbound(): void {
    if (!this.canManage || this.busy) return;
    if (!confirm('¿Desea consultar y procesar ahora los archivos recibidos?')) return;
    this.run(() => this.api.executeInbound(), () => { this.notifications.success('La consulta de archivos recibidos finalizó.'); this.refresh(); });
  }
  retry(): void { if (this.canManage && !this.busy && this.selected?.canRetry && confirm('¿Desea reintentar esta operación?')) this.action(() => this.api.retry(this.selected!.id)); }
  reprocess(): void { if (this.canManage && !this.busy && this.selected?.canReprocess && confirm('¿Desea reprocesar este archivo con las protecciones de duplicados vigentes?')) this.action(() => this.api.reprocess(this.selected!.id)); }
  archive(): void { if (this.canManage && !this.busy && this.selected?.canArchive && confirm('¿Desea archivar este archivo?')) this.action(() => this.api.archive(this.selected!.id)); }
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
  outcome(value: string): string { return ({ Started: 'Iniciado', Succeeded: 'Correcto', Ignored: 'Ignorado' } as Record<string, string>)[value] ?? this.status(value); }
  lastSuccessfulStep(detail: TransferDetail) {
    return [...detail.history].reverse().find(x => x.result === 'Succeeded' || x.result === 'Processed');
  }
  guidance(detail: TransferDetail): string {
    if (detail.status === 'Uncertain') return detail.canRetry
      ? 'Entrega sin confirmar. El reintento controlado verifica la identidad del archivo en la frontera administrada.'
      : 'Entrega sin confirmar. Revise la evidencia con el responsable MFT; el reintento no está disponible.';
    if (detail.canRetry) return 'Reintento disponible sobre el mismo contenido retenido.';
    if (detail.canReprocess) return 'Reproceso disponible mediante la ingestión existente.';
    if (detail.status === 'RetryPending') return 'Reintento no disponible. Revise el límite de intentos, el contenido retenido y el detalle de ingestión si corresponde.';
    if (detail.status === 'Failed' || detail.status === 'Rejected') return 'Requiere revisión operativa. Consulte el diagnóstico y el historial; no hay reintento habilitado.';
    if (detail.status === 'Transferred') return 'Archivo entregado a la frontera MFT. La entrega externa y la respuesta de la cámara requieren su propia evidencia.';
    return '';
  }
  status(value: string): string { return ({ Ready: 'Pendiente', InProgress: 'En proceso', Transferred: 'Enviado', Received: 'Recibido', Processed: 'Procesado', Rejected: 'Rechazado', Duplicate: 'Duplicado', RetryPending: 'Pendiente de reintento', Uncertain: 'Resultado por confirmar', Failed: 'Fallido', Retired: 'Retirado' } as Record<string, string>)[value] ?? value; }
  event(value: string): string { return ({ OutboundPrepared: 'Archivo preparado', OutboundAttempt: 'Intento de envío', InboundClaimed: 'Archivo recibido', InboundProcessingStarted: 'Procesamiento iniciado', InboundProcessingFinished: 'Procesamiento finalizado', DuplicateDetected: 'Duplicado detectado', Archived: 'Archivo archivado', Retired: 'Archivo retirado', Downloaded: 'Archivo descargado', ReprocessStarted: 'Reproceso iniciado', ReprocessFinished: 'Reproceso finalizado' } as Record<string, string>)[value] ?? value; }
  private action(request: () => ReturnType<AchColombiaFileExchangeService['retry']>): void { this.run(request, detail => { this.selected = detail; this.notifications.success('Operación completada.'); this.refresh(); }); }
  private run<T>(request: () => import('rxjs').Observable<T>, success: (value: T) => void): void {
    this.busy = true; this.error = '';
    request().pipe(finalize(() => this.busy = false)).subscribe({ next: success, error: error => {
      this.error = error?.error?.detail ?? 'No fue posible completar la operación.';
      this.notifications.error(this.error);
    }});
  }
}
