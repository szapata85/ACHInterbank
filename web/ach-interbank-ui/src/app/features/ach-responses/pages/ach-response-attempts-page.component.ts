import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import type { UiEtiquetaEstado } from '../../../shared/components/ui/ui-etiqueta-estado.component';
import { SharedModule } from '../../../shared/shared.module';
import { AchResponseNotificationAttemptResponse } from '../models/ach-responses.models';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { formatAchDate, formatAchValue } from '../utils/ach-response-formatters';
import { formatAchNotificationStatus } from '../utils/ach-response-status.utils';

@Component({
  selector: 'app-ach-response-attempts-page',
  standalone: true,
  imports: [CommonModule, RouterModule, SharedModule, MatButtonModule, MatTableModule],
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

  readonly displayedColumns = [
    'numeroIntento',
    'estadoNotificacion',
    'idCanal',
    'nombreCanal',
    'idTransaccion',
    'idEstado',
    'causal',
    'idTransaccionServicioExterno',
    'descripcionCausal',
    'existeError',
    'codigoError',
    'descripcionError',
    'errorTecnico',
    'fechaCreacion',
    'fechaEnvio'
  ];

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

    this.api.getAttempts(id).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: (response) => {
        this.attempts = response ?? [];
      },
      error: () => {
        this.error = true;
        this.attempts = [];
        this.notifications.error('No fue posible cargar los intentos de notificación ACH');
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

  formatValue(value: unknown): string { return formatAchValue(value); }

  formatDate(value: string | null | undefined): string { return formatAchDate(value); }

  formatNotificationStatus(status: string | null | undefined): string { return formatAchNotificationStatus(status); }

  getNotificationStatusState(status: string | null | undefined): UiEtiquetaEstado {
    if (status === 'Exitosa') return 'exitoso';
    if (status === 'Pendiente' || status === 'PendienteReintento' || status === 'RequiereRevisionManual') return 'pendiente';
    if (status === 'ErrorFuncional' || status === 'ErrorTecnico') return 'fallido';
    return 'inactivo';
  }
}
