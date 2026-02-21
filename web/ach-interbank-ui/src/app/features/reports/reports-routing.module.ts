import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ReportsHomeComponent } from './components/reports-home.component';
import { TraceabilityReportComponent } from './components/traceability-report.component';

const routes: Routes = [
  {
    path: '',
    component: ReportsHomeComponent,
    data: { breadcrumb: 'Reportes', title: 'Reportes' }
  },
  {
    path: 'traceability',
    component: TraceabilityReportComponent,
    data: { breadcrumb: 'Trazabilidad ACH', title: 'Reporte de trazabilidad ACH', permissions: ['CanReadAch'] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ReportsRoutingModule {}

