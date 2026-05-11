import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { forkJoin, map, Observable } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { AchResponseSearchRequest } from '../models/ach-responses.models';
import { AchResponsesApiService } from '../services/ach-responses-api.service';
import { calculateAchRate, normalizeAchFilter } from '../utils/ach-response-formatters';

type AchDashboardKpi = {
  key: string;
  titulo: string;
  valor: number;
  descripcion: string;
  estado?: string;
  clase: string;
  ruta?: string;
};

@Component({
  selector: 'app-ach-response-dashboard-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, SharedModule],
  templateUrl: './ach-response-dashboard-page.component.html',
  styleUrls: ['./ach-response-dashboard-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchResponseDashboardPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly api = inject(AchResponsesApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;
  error = false;

  readonly filtrosForm = this.fb.group({
    fechaDesde: [''],
    fechaHasta: [''],
    tipoRespuesta: ['']
  });

  readonly tiposRespuesta: Array<'' | 'Prenota' | 'Transaccion'> = ['', 'Prenota', 'Transaccion'];

  kpis: AchDashboardKpi[] = [];

  ngOnInit(): void {
    this.loadDashboard();
  }

  applyFilters(): void {
    this.loadDashboard();
  }

  clearFilters(): void {
    this.filtrosForm.setValue({ fechaDesde: '', fechaHasta: '', tipoRespuesta: '' });
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading = true;
    this.error = false;
    this.cdr.markForCheck();

    forkJoin({
      totalRespuestas: this.countByStatus(),
      recibidas: this.countByStatus('Recibida'),
      homologadas: this.countByStatus('Homologada'),
      notificadas: this.countByStatus('Notificada'),
      noHomologadas: this.countByStatus('NoHomologada'),
      revisionManual: this.countByStatus('RequiereRevisionManual'),
      pendientesReintento: this.countByStatus('PendienteReintento'),
      erroresFuncionales: this.countByStatus('ErrorFuncional'),
      duplicadas: this.countByStatus('Duplicada')
    }).subscribe({
      next: (counts) => {
        this.kpis = [
          { key: 'totalRespuestas', titulo: 'Total respuestas', valor: counts.totalRespuestas, descripcion: 'Total de respuestas recibidas.', clase: 'kpi-neutro', ruta: '/ach-responses' },
          { key: 'recibidas', titulo: 'Recibidas', valor: counts.recibidas, descripcion: 'Respuestas en estado recibida.', clase: 'kpi-neutro', ruta: '/ach-responses' },
          { key: 'homologadas', titulo: 'Homologadas', valor: counts.homologadas, descripcion: 'Respuestas homologadas.', clase: 'kpi-exitoso', ruta: '/ach-responses' },
          { key: 'notificadas', titulo: 'Notificadas', valor: counts.notificadas, descripcion: 'Respuestas notificadas.', clase: 'kpi-exitoso', ruta: '/ach-responses' },
          { key: 'noHomologadas', titulo: 'No homologadas', valor: counts.noHomologadas, descripcion: 'Casos sin homologación.', clase: 'kpi-advertencia', ruta: '/ach-responses/manual-review' },
          { key: 'revisionManual', titulo: 'Revisión manual', valor: counts.revisionManual, descripcion: 'Casos que requieren revisión manual.', clase: 'kpi-advertencia', ruta: '/ach-responses/manual-review' },
          { key: 'pendientesReintento', titulo: 'Pendientes reintento', valor: counts.pendientesReintento, descripcion: 'Casos pendientes de reintento.', clase: 'kpi-advertencia', ruta: '/ach-responses/manual-review' },
          { key: 'erroresFuncionales', titulo: 'Errores funcionales', valor: counts.erroresFuncionales, descripcion: 'Casos con error funcional.', clase: 'kpi-error', ruta: '/ach-responses/manual-review' },
          { key: 'duplicadas', titulo: 'Duplicadas', valor: counts.duplicadas, descripcion: 'Respuestas duplicadas.', clase: 'kpi-neutro', ruta: '/ach-responses' }
        ];
      },
      error: () => {
        this.error = true;
        this.kpis = [];
        this.notifications.error('No fue posible cargar el dashboard operativo ACH.');
      },
      complete: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  openKpi(kpi: AchDashboardKpi): void {
    if (kpi.ruta) {
      this.router.navigate([kpi.ruta]);
    }
  }

  getKpiValue(key: string): number {
    return this.kpis.find((item) => item.key === key)?.valor ?? 0;
  }

  getCriticalTotal(): number {
    return this.getKpiValue('noHomologadas') + this.getKpiValue('revisionManual') + this.getKpiValue('pendientesReintento') + this.getKpiValue('erroresFuncionales');
  }

  calculateRate(value: number, total: number): string { return calculateAchRate(value, total); }

  formatNumber(value: number): string {
    return (value ?? 0).toLocaleString('es-CO');
  }

  normalize(value: string | null | undefined): string | undefined { return normalizeAchFilter(value); }

  private countByStatus(status?: string): Observable<number> {
    const raw = this.filtrosForm.getRawValue();
    const request: AchResponseSearchRequest = {
      fechaDesde: this.normalize(raw.fechaDesde),
      fechaHasta: this.normalize(raw.fechaHasta),
      tipoRespuesta: this.normalize(raw.tipoRespuesta) as 'Prenota' | 'Transaccion' | undefined,
      estadoProcesamiento: status,
      pageNumber: 1,
      pageSize: 1
    };

    return this.api.search(request).pipe(map((response) => response.totalCount ?? 0));
  }
}
