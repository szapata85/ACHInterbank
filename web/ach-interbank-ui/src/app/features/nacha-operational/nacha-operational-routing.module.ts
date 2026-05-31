import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { NachaOperationalDashboardComponent } from './pages/nacha-operational-dashboard.component';
import { NachaOperationalFileDetailComponent } from './pages/nacha-operational-file-detail.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'nacha/operational-dashboard' },
  {
    path: 'nacha/operational-dashboard',
    component: NachaOperationalDashboardComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadAch'],
      breadcrumb: 'Readiness SOAP',
      title: 'Consulta operativa NACHA-M y readiness SOAP'
    }
  },
  {
    path: 'nacha/operational-dashboard/files/:fileId',
    component: NachaOperationalFileDetailComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadAch'],
      breadcrumb: 'Detalle archivo NACHA-M',
      title: 'Detalle operativo NACHA-M read-only'
    }
  },
  {
    path: 'nacha/config-profiles',
    pathMatch: 'full',
    redirectTo: '/nacha-config-admin/perfiles'
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NachaOperationalRoutingModule {}
