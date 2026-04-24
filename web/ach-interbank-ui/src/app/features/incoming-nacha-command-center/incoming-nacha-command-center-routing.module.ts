import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { IncomingNachaIngestionDetailPageComponent } from './pages/incoming-nacha-ingestion-detail-page.component';
import { IncomingNachaIngestionsPageComponent } from './pages/incoming-nacha-ingestions-page.component';
import { IncomingNachaQueueDetailPageComponent } from './pages/incoming-nacha-queue-detail-page.component';
import { IncomingNachaQueuePageComponent } from './pages/incoming-nacha-queue-page.component';

const routes: Routes = [
  {
    path: '',
    component: IncomingNachaIngestionsPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Inbound NACHA', title: 'Command Center inbound NACHA' }
  },
  {
    path: 'ingestions/:id',
    component: IncomingNachaIngestionDetailPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Detalle ingesta', title: 'Detalle de ingesta inbound NACHA' }
  },
  {
    path: 'queue',
    component: IncomingNachaQueuePageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Cola dispatch', title: 'Cola dispatch inbound NACHA' }
  },
  {
    path: 'queue/:id',
    component: IncomingNachaQueueDetailPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Detalle cola', title: 'Detalle de item de cola inbound NACHA' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class IncomingNachaCommandCenterRoutingModule {}
