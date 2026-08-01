import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { NachaSoapUatConsoleComponent } from './pages/nacha-soap-uat-console.component';
import { AchReconciliationConsoleComponent } from './pages/ach-reconciliation-console.component';

export const NACHA_OPERATIONAL_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'nacha/operational-dashboard' },
  {
    path: 'nacha/operational-dashboard',
    pathMatch: 'full',
    redirectTo: '/incoming-nacha-command-center'
  },
  {
    path: 'nacha/operational-dashboard/files/:fileId',
    redirectTo: '/incoming-nacha-command-center/files/:fileId'
  },
  {
    path: 'nacha/soap-uat-console',
    component: NachaSoapUatConsoleComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
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
  imports: [RouterModule.forChild(NACHA_OPERATIONAL_ROUTES)],
  exports: [RouterModule]
})
export class NachaOperationalRoutingModule {}
