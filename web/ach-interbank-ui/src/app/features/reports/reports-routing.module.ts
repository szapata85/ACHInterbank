import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ReportsHomeComponent } from './components/reports-home.component';
import { TraceabilityReportComponent } from './components/traceability-report.component';
import { ReportListPageComponent } from './components/report-list-page.component';
import { ReconciliationReportComponent } from './components/reconciliation-report.component';

const routes: Routes = [
  { path: '', component: ReportsHomeComponent, data: { breadcrumb: 'Reportes', title: 'Reportes' } },
  { path: 'traceability', component: TraceabilityReportComponent, data: { breadcrumb: 'Trazabilidad ACH', title: 'Reporte de trazabilidad ACH', permissions: ['CanReadAch'] } },
  { path: 'sent', component: ReportListPageComponent, data: { title: 'Enviados', reportKey: 'sent', permissions: ['CanReadAch'] } },
  { path: 'received', component: ReportListPageComponent, data: { title: 'Recibidos', reportKey: 'received', permissions: ['CanReadAch'] } },
  { path: 'returns', component: ReportListPageComponent, data: { title: 'Devoluciones', reportKey: 'returns', permissions: ['CanReadAch'] } },
  { path: 'rejections', component: ReportListPageComponent, data: { title: 'Rechazos', reportKey: 'rejections', permissions: ['CanReadAch'] } },
  { path: 'files', component: ReportListPageComponent, data: { title: 'Archivos', reportKey: 'files', permissions: ['CanReadAch'] } },
  { path: 'cycles', component: ReportListPageComponent, data: { title: 'Ciclos', reportKey: 'cycles', permissions: ['CanReadAch'] } },
  { path: 'audit', component: ReportListPageComponent, data: { title: 'Auditoría', reportKey: 'audit', permissions: ['CanReadAch'] } },
  { path: 'history', component: ReportListPageComponent, data: { title: 'Histórico', reportKey: 'history', permissions: ['CanReadAch'] } },
  { path: 'reconciliation', component: ReconciliationReportComponent, data: { title: 'Conciliación', reportKey: 'reconciliation', permissions: ['CanReadAch'] } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ReportsRoutingModule {}
