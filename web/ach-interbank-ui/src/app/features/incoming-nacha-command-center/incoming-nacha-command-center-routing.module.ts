import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { NachaOperationalDashboardComponent } from '../nacha-operational/pages/nacha-operational-dashboard.component';
import { NachaOperationalFileDetailComponent } from '../nacha-operational/pages/nacha-operational-file-detail.component';
import { IncomingNachaObservabilityPageComponent } from './pages/incoming-nacha-observability-page.component';
import { IncomingNachaQueueDetailPageComponent } from './pages/incoming-nacha-queue-detail-page.component';
import { IncomingNachaQueuePageComponent } from './pages/incoming-nacha-queue-page.component';
import { IncomingNachaOrphansPageComponent } from './pages/incoming-nacha-orphans-page.component';

const routes: Routes = [
  {
    path: '',
    component: NachaOperationalDashboardComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Seguimiento de archivos NACHA-M', title: 'Seguimiento de archivos NACHA-M' }
  },
  {
    path: 'files/:fileId',
    component: NachaOperationalFileDetailComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Detalle del archivo', title: 'Trazabilidad del archivo NACHA-M' }
  },
  {
    path: 'ingestions/:fileId',
    component: NachaOperationalFileDetailComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Detalle del archivo', title: 'Trazabilidad del archivo NACHA-M' }
  },
  {
    path: 'observability',
    component: IncomingNachaObservabilityPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Indicadores operativos', title: 'Indicadores operativos NACHA-M' }
  },
  {
    path: 'orphan-resolution',
    component: IncomingNachaOrphansPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Devoluciones sin relación', title: 'Resolver devoluciones sin relación' }
  },
  {
    path: 'queue',
    component: IncomingNachaQueuePageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Cola de procesamiento', title: 'Cola de procesamiento NACHA-M' }
  },
  {
    path: 'queue/:id',
    component: IncomingNachaQueueDetailPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Detalle de procesamiento', title: 'Detalle de procesamiento NACHA-M' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class IncomingNachaCommandCenterRoutingModule {}
