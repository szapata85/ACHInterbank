import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { NachaOperationalDashboardComponent } from './pages/nacha-operational-dashboard.component';
import { NachaOperationalFileDetailComponent } from './pages/nacha-operational-file-detail.component';
import { NachaSoapUatConsoleComponent } from './pages/nacha-soap-uat-console.component';
import { AchReconciliationConsoleComponent } from './pages/ach-reconciliation-console.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'nacha/operational-dashboard' },
  {
    path: 'nacha/operational-dashboard',
    component: NachaOperationalDashboardComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadAch'],
      breadcrumb: 'Preparación SOAP',
      title: 'Consulta operativa NACHA-M y preparación SOAP'
    }
  },
  {
    path: 'nacha/operational-dashboard/files/:fileId',
    component: NachaOperationalFileDetailComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadAch'],
      breadcrumb: 'Detalle archivo NACHA-M',
      title: 'Detalle operativo NACHA-M solo lectura'
    }
  },
  {
    path: 'nacha/soap-uat-console',
    component: NachaSoapUatConsoleComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadAch'],
      breadcrumb: 'Consola SOAP/UAT',
      title: 'Consola SOAP/UAT solo lectura'
    }
  },
  {
    path: 'reconciliation',
    component: AchReconciliationConsoleComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadAch'],
      breadcrumb: 'Conciliación ACH',
      title: 'Consola de conciliación ACH solo lectura'
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
