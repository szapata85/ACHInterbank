import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import {
  IncomingNachaIntegrationExecution,
  IncomingNachaManualActionResult,
  IncomingNachaProcessingEvent,
  IncomingNachaQueueDetail,
  IncomingNachaQueueListItem
} from '../models/incoming-nacha-command-center.models';
import { IncomingNachaCommandCenterApiService } from '../services/incoming-nacha-command-center-api.service';

type ManualActionType = 'retry' | 'unblock' | 'requeue' | 'mark-failed-final';

@Component({
  selector: 'app-incoming-nacha-queue-detail-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, SharedModule],
  templateUrl: './incoming-nacha-queue-detail-page.component.html',
  styleUrls: ['./incoming-nacha-queue-detail-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IncomingNachaQueueDetailPageComponent implements OnInit {
  private readonly api = inject(IncomingNachaCommandCenterApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly notifications = inject(NotificationService);
  private readonly auth = inject(AuthService);

  cargando = false;
  ejecutando = false;
  error = '';
  resultadoAccion = '';
  detalle?: IncomingNachaQueueDetail;
  modalAbierto = false;
  accionSeleccionada?: ManualActionType;

  readonly accionForm = this.fb.group({
    justification: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(600)]]
  });

  readonly columnasEvents: ColDef<IncomingNachaProcessingEvent>[] = [
    { field: 'eventType', headerName: 'Evento', minWidth: 150 },
    { field: 'eventStatus', headerName: 'Estado', minWidth: 120 },
    { field: 'raisedBy', headerName: 'Actor', minWidth: 150 },
    { headerName: 'Fecha', minWidth: 170, valueGetter: (p) => this.formatDate(p.data?.occurredAtUtc) },
    { field: 'message', headerName: 'Mensaje', minWidth: 380 }
  ];

  readonly columnasExec: ColDef<IncomingNachaIntegrationExecution>[] = [
    { field: 'methodName', headerName: 'Método', minWidth: 170 },
    { field: 'responseCode', headerName: 'Código', minWidth: 110 },
    { field: 'responseMessage', headerName: 'Respuesta', minWidth: 260 },
    { field: 'isSuccess', headerName: 'Éxito', minWidth: 95, valueGetter: (p) => (p.data?.isSuccess ? 'Sí' : 'No') },
    { field: 'isRetryable', headerName: 'Retryable', minWidth: 110, valueGetter: (p) => (p.data?.isRetryable ? 'Sí' : 'No') },
    { headerName: 'Inicio', minWidth: 170, valueGetter: (p) => this.formatDate(p.data?.startedAtUtc) },
    { headerName: 'Fin', minWidth: 170, valueGetter: (p) => this.formatDate(p.data?.finishedAtUtc) }
  ];

  get queue(): IncomingNachaQueueListItem | undefined {
    return this.detalle?.queue;
  }

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error = 'No se recibió identificador de item de cola.';
      return;
    }

    this.cargando = true;
    this.error = '';
    this.api.getQueueDetail(id).pipe(finalize(() => {
      this.cargando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (detalle) => {
        this.detalle = detalle;
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'No fue posible cargar el detalle de cola inbound.';
      }
    });
  }

  abrirAccion(action: ManualActionType): void {
    if (!this.estaPermitida(action)) {
      this.notifications.warning('La acción no está permitida por backend para el estado actual.');
      return;
    }

    this.accionSeleccionada = action;
    this.resultadoAccion = '';
    this.accionForm.reset({ justification: '' });
    this.modalAbierto = true;
  }

  cancelarAccion(): void {
    this.modalAbierto = false;
    this.accionSeleccionada = undefined;
    this.ejecutando = false;
  }

  confirmarAccion(): void {
    if (!this.accionSeleccionada || !this.queue) {
      return;
    }

    if (this.accionForm.invalid) {
      this.accionForm.markAllAsTouched();
      return;
    }

    const justification = this.accionForm.controls.justification.value?.trim() ?? '';
    const payload = {
      justification,
      idempotencyKey: this.createIdempotencyKey(this.accionSeleccionada, this.queue.id)
    };

    const request$ = this.accionSeleccionada === 'retry'
      ? this.api.retry(this.queue.id, payload)
      : this.accionSeleccionada === 'unblock'
        ? this.api.unblock(this.queue.id, payload)
        : this.accionSeleccionada === 'requeue'
          ? this.api.requeue(this.queue.id, payload)
          : this.api.markFailedFinal(this.queue.id, payload);

    this.ejecutando = true;
    request$.pipe(finalize(() => {
      this.ejecutando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (result) => {
        this.procesarResultado(result);
        this.modalAbierto = false;
        this.cargar();
      },
      error: (err) => {
        this.resultadoAccion = err?.error?.message ?? 'No fue posible aplicar la acción manual solicitada.';
        this.notifications.error(this.resultadoAccion);
      }
    });
  }

  irAIngesta(): void {
    if (!this.detalle) return;
    this.router.navigate(['/incoming-nacha-command-center/ingestions', this.detalle.ingestion.id]);
  }

  volverCola(): void {
    this.router.navigate(['/incoming-nacha-command-center/queue']);
  }

  estaPermitida(action: ManualActionType): boolean {
    const allowed = this.queue?.allowedActions;
    if (!allowed) return false;

    const hasBusinessPermission = action === 'retry'
      ? allowed.canRetry
      : action === 'unblock'
        ? allowed.canUnblock
        : action === 'requeue'
          ? allowed.canRequeue
          : allowed.canMarkFailedFinal;

    const hasUserPermission = this.auth.hasPermission(['CanManageAch', this.mapPermission(action)]);
    return hasBusinessPermission && hasUserPermission;
  }

  etiquetaAccion(action: ManualActionType): string {
    return action === 'mark-failed-final' ? 'mark-failed-final' : action;
  }

  private procesarResultado(result: IncomingNachaManualActionResult): void {
    const outcome = result.isIdempotentReplay
      ? 'Replay idempotente'
      : result.currentStatus === result.previousStatus
        ? 'Rejected'
        : 'Applied';

    this.resultadoAccion = `${outcome}: ${result.message}`;
    if (result.isIdempotentReplay) {
      this.notifications.info(this.resultadoAccion);
      return;
    }

    this.notifications.success(this.resultadoAccion);
  }

  private createIdempotencyKey(action: ManualActionType, queueId: string): string {
    const sanitized = action.replace(/[^a-z-]/g, '');
    const random = Math.random().toString(36).slice(2, 10);
    return `inbound-${sanitized}-${queueId}-${Date.now()}-${random}`;
  }

  private mapPermission(action: ManualActionType): string {
    switch (action) {
      case 'retry': return 'CanRetryIncoming';
      case 'unblock': return 'CanUnblockIncoming';
      case 'requeue': return 'CanRequeueIncoming';
      default: return 'CanMarkIncoming';
    }
  }

  private formatDate(value?: string | null): string {
    return value ? new Date(value).toLocaleString('es-CO') : '—';
  }
}
