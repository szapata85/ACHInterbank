import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { AchResponseAuditModel, AchResponseDetailResponse, AchResponseReprocessModel } from '../models/ach-responses.models';
import { HttpErrorResponse } from '@angular/common/http';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { formatAchDate, formatAchValue } from '../utils/ach-response-formatters';
import { formatAchNotificationStatus, formatAchProcessingStatus, getAchNotificationStatusClass, getAchProcessingStatusClass } from '../utils/ach-response-status.utils';

@Component({
  selector: 'app-ach-response-detail-page',
  standalone: true,
  imports: [CommonModule, RouterModule, SharedModule],
  templateUrl: './ach-response-detail-page.component.html',
  styleUrls: ['./ach-response-detail-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchResponseDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(AchResponsesApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  detail: AchResponseDetailResponse | null = null;
  loading = false;
  error = false;
  responseId: string | null = null;
  reprocessReason = '';
  reprocessing = false;
  auditEntries: AchResponseAuditModel[] = [];
  auditVisible = false;
  reprocessAttempts: AchResponseReprocessModel[] = [];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.responseId = id;

    if (!id) {
      this.error = true;
      this.notifications.error('No se encontró el identificador de la respuesta ACH');
      this.cdr.markForCheck();
      return;
    }

    this.loadDetail(id);
    this.loadReprocessAttempts(id);
  }

  loadReprocessAttempts(id: string): void {
    this.api.getReprocessAttempts(id).subscribe({
      next: items => {
        this.reprocessAttempts = items ?? [];
        this.cdr.markForCheck();
      },
      error: () => this.notifications.error('No fue posible cargar el historial de reprocesos.')
    });
  }

  loadDetail(id: string): void {
    this.loading = true;
    this.error = false;
    this.cdr.markForCheck();

    this.api.getDetail(id).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: (response) => {
        this.detail = response;
      },
      error: () => {
        this.error = true;
        this.detail = null;
        this.notifications.error('No fue posible cargar el detalle de la respuesta ACH');
      }
    });
  }

  backToList(): void {
    this.router.navigate(['/ach-responses']);
  }

  goToAttempts(): void {
    if (!this.detail) return;
    this.router.navigate(['/ach-responses', this.detail.id, 'notification-attempts']);
  }

  canReprocess(): boolean {
    return !!this.detail && ['ErrorTecnico', 'PendienteReintento', 'NoHomologada', 'ErrorFuncional', 'Resuelta']
      .includes(this.detail.estadoProcesamiento);
  }

  requestReprocess(): void {
    if (!this.detail || !this.canReprocess()) return;
    if (this.reprocessReason.trim().length < 5) {
      this.notifications.error('La justificación de reproceso es obligatoria.');
      return;
    }
    this.reprocessing = true;
    const commandId = globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-0000-4000-8000-000000000000`;
    this.api.requestReprocess(this.detail.id, commandId, this.detail.version, this.reprocessReason.trim())
      .pipe(finalize(() => { this.reprocessing = false; this.cdr.markForCheck(); }))
      .subscribe({ next: () => {
        this.notifications.success('Reproceso solicitado y pendiente de ejecución gobernada.');
        this.reprocessReason = '';
        this.loadDetail(this.detail!.id);
        this.loadReprocessAttempts(this.detail!.id);
      }, error: (error: HttpErrorResponse) => {
        if (error.status === 409) {
          this.notifications.warning('La respuesta cambió o ya tiene un reproceso activo. Se recargó el detalle.');
          this.loadDetail(this.detail!.id);
        } else this.notifications.error(typeof error.error?.detail === 'string' ? error.error.detail : 'No fue posible solicitar el reproceso.');
      }});
  }

  formatReprocessStatus(status: string): string {
    return ({
      Pending: 'Pendiente de ejecución',
      Running: 'En ejecución',
      Completed: 'Completado',
      FailedFunctional: 'Requiere revisión',
      FailedTechnical: 'Error técnico'
    } as Record<string, string>)[status] ?? status;
  }

  getReprocessStatusClass(status: string): string {
    if (status === 'Completed') return 'estado-exitoso';
    if (status === 'Pending' || status === 'Running' || status === 'FailedFunctional') return 'estado-advertencia';
    if (status === 'FailedTechnical') return 'estado-error';
    return 'estado-neutro';
  }

  toggleAudit(): void {
    if (!this.detail) return;
    this.auditVisible = !this.auditVisible;
    if (!this.auditVisible || this.auditEntries.length > 0) return;
    this.api.getResponseAudit(this.detail.id).subscribe({
      next: (items) => { this.auditEntries = items ?? []; this.cdr.markForCheck(); },
      error: () => this.notifications.error('No fue posible cargar la auditoría de la respuesta.')
    });
  }

  formatValue(value: unknown): string { return formatAchValue(value); }

  formatDate(value: string | null | undefined): string { return formatAchDate(value); }

  formatProcessingStatus(status: string | null | undefined): string {
    return formatAchProcessingStatus(status);
  }

  getProcessingStatusClass(status: string | null | undefined): string {
    if (!status) return 'estado-neutro';
    if (status === 'Notificada' || status === 'Homologada') return 'estado-exitoso';
    if (status === 'PendienteReintento' || status === 'RequiereRevisionManual' || status === 'NoHomologada') return 'estado-advertencia';
    if (status === 'ErrorFuncional') return 'estado-error';
    return 'estado-neutro';
  }

  formatNotificationStatus(status: string | null | undefined): string {
    return formatAchNotificationStatus(status);
  }

  getNotificationStatusClass(status: string | null | undefined): string {
    if (!status) return 'estado-neutro';
    if (status === 'Exitosa') return 'estado-exitoso';
    if (status === 'Pendiente' || status === 'PendienteReintento' || status === 'RequiereRevisionManual') return 'estado-advertencia';
    if (status === 'ErrorFuncional' || status === 'ErrorTecnico') return 'estado-error';
    return 'estado-neutro';
  }
}
