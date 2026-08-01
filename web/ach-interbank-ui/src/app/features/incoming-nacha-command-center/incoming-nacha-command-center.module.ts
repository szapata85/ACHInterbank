import { NgModule } from '@angular/core';
import { IncomingNachaCommandCenterRoutingModule } from './incoming-nacha-command-center-routing.module';
import { NachaOperationalDashboardComponent } from '../nacha-operational/pages/nacha-operational-dashboard.component';
import { NachaOperationalFileDetailComponent } from '../nacha-operational/pages/nacha-operational-file-detail.component';
import { IncomingNachaObservabilityPageComponent } from './pages/incoming-nacha-observability-page.component';
import { IncomingNachaQueueDetailPageComponent } from './pages/incoming-nacha-queue-detail-page.component';
import { IncomingNachaQueuePageComponent } from './pages/incoming-nacha-queue-page.component';

@NgModule({
  imports: [
    IncomingNachaCommandCenterRoutingModule,
    NachaOperationalDashboardComponent,
    NachaOperationalFileDetailComponent,
    IncomingNachaObservabilityPageComponent,
    IncomingNachaQueuePageComponent,
    IncomingNachaQueueDetailPageComponent
  ]
})
export class IncomingNachaCommandCenterModule {}
