import { NgModule } from '@angular/core';
import { IncomingNachaCommandCenterRoutingModule } from './incoming-nacha-command-center-routing.module';
import { IncomingNachaIngestionDetailPageComponent } from './pages/incoming-nacha-ingestion-detail-page.component';
import { IncomingNachaIngestionsPageComponent } from './pages/incoming-nacha-ingestions-page.component';
import { IncomingNachaObservabilityPageComponent } from './pages/incoming-nacha-observability-page.component';
import { IncomingNachaQueueDetailPageComponent } from './pages/incoming-nacha-queue-detail-page.component';
import { IncomingNachaQueuePageComponent } from './pages/incoming-nacha-queue-page.component';

@NgModule({
  imports: [
    IncomingNachaCommandCenterRoutingModule,
    IncomingNachaIngestionsPageComponent,
    IncomingNachaIngestionDetailPageComponent,
    IncomingNachaObservabilityPageComponent,
    IncomingNachaQueuePageComponent,
    IncomingNachaQueueDetailPageComponent
  ]
})
export class IncomingNachaCommandCenterModule {}
