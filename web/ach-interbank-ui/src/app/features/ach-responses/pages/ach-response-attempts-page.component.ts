import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { AchResponseNotificationAttemptResponse } from '../models/ach-responses.models';
import { AchResponsesApiService } from '../services/ach-responses-api.service';

@Component({
  selector: 'app-ach-response-attempts-page',
  standalone: true,
  imports: [CommonModule, RouterModule, SharedModule],
  templateUrl: './ach-response-attempts-page.component.html',
  styleUrls: ['./ach-response-attempts-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchResponseAttemptsPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(AchResponsesApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  responseId: string | null = null;
  attempts: AchResponseNotificationAttemptResponse[] = [];
  loading = false;
  error = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.responseId = id;

    if (!id) {
      this.error = true;
      this.notifications.error('No se encontró el identificador de la respuesta ACH');
      this.cdr.markForCheck();
      return;
    }

    this.loadAttempts(id);
  }

  loadAttempts(id: string): void {
    this.loading = true;
    this.error = false;
    this.cdr.markForCheck();

    this.api.getAttempts(id).subscribe({
      next: (response) => {
        this.attempts = response ?? [];
      },
      error: () => {
        this.error = true;
        this.attempts = [];
        this.notifications.error('No fue posible cargar los intentos de notificación ACH');
      },
      complete: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  retryLoad(): void {
    if (this.responseId) {
      this.loadAttempts(this.responseId);
    }
  }

  backToDetail(): void {
    if (this.responseId) {
      this.router.navigate(['/ach-responses', this.responseId]);
      return;
    }

    this.router.navigate(['/ach-responses']);
  }

  backToList(): void {
    this.router.navigate(['/ach-responses']);
  }

  formatValue(value: unknown): string {
    if (value === null || value === undefined || value === '') return '-';
    if (typeof value === 'boolean') return value ? 'Sí' : 'No';
    return String(value);
  }

  formatDate(value: string | null | undefined): string {
    if (!value) return '-';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleString('es-CO');
  }

  formatNotificationStatus(status: string | null | undefined): string {
    return status?.trim() ? status : '-';
  }

  getNotificationStatusClass(status: string | null | undefined): string {
    if (!status) return 'estado-neutro';
    if (status === 'Exitosa') return 'estado-exitoso';
    if (status === 'Pendiente' || status === 'PendienteReintento' || status === 'RequiereRevisionManual') return 'estado-advertencia';
    if (status === 'ErrorFuncional' || status === 'ErrorTecnico') return 'estado-error';
    return 'estado-neutro';
  }
}
