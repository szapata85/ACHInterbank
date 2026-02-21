import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { ReportsRoutingModule } from './reports-routing.module';
import { ReportsHomeComponent } from './components/reports-home.component';
import { TraceabilityReportComponent } from './components/traceability-report.component';

@NgModule({
  imports: [SharedModule, ReportsRoutingModule, ReportsHomeComponent, TraceabilityReportComponent]
})
export class ReportsModule {}

