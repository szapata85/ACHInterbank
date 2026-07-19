import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { AchResponseDashboardRequest, AchResponseDashboardResponse } from '../models/ach-responses.models';
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

    this.api.getDashboard(this.buildDashboardRequest()).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: (counts) => {
        this.kpis = this.mapKpis(counts);
      },
      error: () => {
        this.error = true;
        this.kpis = [];
        this.notifications.error('No fue posible cargar el dashboard operativo ACH.');
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

  private buildDashboardRequest(): AchResponseDashboardRequest {
    const raw = this.filtrosForm.getRawValue();
    return {
      fechaDesde: this.normalize(raw.fechaDesde),
      fechaHasta: this.normalize(raw.fechaHasta),
      tipoRespuesta: this.normalize(raw.tipoRespuesta) as 'Prenota' | 'Transaccion' | undefined
    };
  }

  private mapKpis(counts: AchResponseDashboardResponse): AchDashboardKpi[] {
    return [
      { key: 'totalRespuestas', titulo: 'Total respuestas', valor: counts.totalRespuestas ?? 0, descripcion: 'Total de respuestas recibidas.', clase: 'kpi-neutro', ruta: '/ach-responses' },
      { key: 'recibidas', titulo: 'Recibidas', valor: counts.recibidas ?? 0, descripcion: 'Respuestas en estado recibida.', clase: 'kpi-neutro', ruta: '/ach-responses' },
      { key: 'homologadas', titulo: 'Homologadas', valor: counts.homologadas ?? 0, descripcion: 'Respuestas homologadas.', clase: 'kpi-exitoso', ruta: '/ach-responses' },
      { key: 'notificadas', titulo: 'Notificadas', valor: counts.notificadas ?? 0, descripcion: 'Respuestas notificadas.', clase: 'kpi-exitoso', ruta: '/ach-responses' },
      { key: 'noHomologadas', titulo: 'No homologadas', valor: counts.noHomologadas ?? 0, descripcion: 'Casos sin homologación.', clase: 'kpi-advertencia', ruta: '/ach-responses/manual-review' },
      { key: 'revisionManual', titulo: 'Revisión manual', valor: counts.revisionManual ?? 0, descripcion: 'Casos que requieren revisión manual.', clase: 'kpi-advertencia', ruta: '/ach-responses/manual-review' },
      { key: 'pendientesReintento', titulo: 'Pendientes reintento', valor: counts.pendientesReintento ?? 0, descripcion: 'Casos pendientes de reintento.', clase: 'kpi-advertencia', ruta: '/ach-responses/manual-review' },
      { key: 'erroresFuncionales', titulo: 'Errores funcionales', valor: counts.erroresFuncionales ?? 0, descripcion: 'Casos con error funcional.', clase: 'kpi-error', ruta: '/ach-responses/manual-review' },
      { key: 'duplicadas', titulo: 'Duplicadas', valor: counts.duplicadas ?? 0, descripcion: 'Respuestas duplicadas.', clase: 'kpi-neutro', ruta: '/ach-responses' }
    ];
  }
}
