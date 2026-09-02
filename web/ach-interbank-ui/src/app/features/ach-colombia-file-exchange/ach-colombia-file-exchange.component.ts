import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { AchColombiaFileExchangeService } from './ach-colombia-file-exchange.service';
import { ExecutionOrigin, TransferDetail, TransferDirection, TransferStatus, TransferSummary } from './ach-colombia-file-exchange.models';

@Component({
  selector: 'app-ach-colombia-file-exchange',
  standalone: true,
  imports: [CommonModule, FormsModule],
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
  filter: { from: string; to: string; direction: TransferDirection | ''; status: TransferStatus | ''; executionOrigin: ExecutionOrigin | ''; cycleId: string } =
    { from: '', to: '', direction: '', status: '', executionOrigin: '', cycleId: '' };
  cycleId = '';
  busy = false;
  error = '';

  ngOnInit(): void { this.refresh(); }
  refresh(): void { this.run(() => this.api.list(this.filter), rows => this.rows = rows); }
  open(row: TransferSummary): void { this.run(() => this.api.detail(row.id), detail => this.selected = detail); }
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
  retry(): void { if (this.canManage && !this.busy && this.selected && confirm('¿Desea reintentar esta operación?')) this.action(() => this.api.retry(this.selected!.id)); }
  reprocess(): void { if (this.canManage && !this.busy && this.selected && confirm('¿Desea reprocesar este archivo con las protecciones de duplicados vigentes?')) this.action(() => this.api.reprocess(this.selected!.id)); }
  archive(): void { if (this.canManage && !this.busy && this.selected && confirm('¿Desea archivar este archivo?')) this.action(() => this.api.archive(this.selected!.id)); }
  retire(): void {
    if (!this.canManage || this.busy || !this.selected || !confirm('El archivo dejará de estar disponible en el área operativa, pero su historial y trazabilidad se conservarán. ¿Desea continuar?')) return;
    const reason = prompt('Indique la razón del retiro:')?.trim();
    if (reason) this.action(() => this.api.retire(this.selected!.id, reason));
  }
  download(): void {
    if (!this.selected) return;
    this.run(() => this.api.download(this.selected!.id), response => {
      const url = URL.createObjectURL(response.body!); const link = document.createElement('a');
      link.href = url; link.download = this.selected!.fileName; link.click(); URL.revokeObjectURL(url);
    });
  }
  direction(value: string): string { return value === 'Outbound' ? 'Envío' : 'Recepción'; }
  origin(value: string): string { return value === 'Automatic' ? 'Automática' : 'Manual'; }
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
