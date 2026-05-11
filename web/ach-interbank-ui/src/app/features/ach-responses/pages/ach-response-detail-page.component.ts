import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { AchResponseDetailResponse } from '../models/ach-responses.models';
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
  }

  loadDetail(id: string): void {
    this.loading = true;
    this.error = false;
    this.cdr.markForCheck();

    this.api.getDetail(id).subscribe({
      next: (response) => {
        this.detail = response;
      },
      error: () => {
        this.error = true;
        this.detail = null;
        this.notifications.error('No fue posible cargar el detalle de la respuesta ACH');
      },
      complete: () => {
        this.loading = false;
        this.cdr.markForCheck();
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
