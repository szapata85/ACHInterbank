import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AchCycleListComponent } from './components/ach-cycle-list.component';
import { AchCycleFormComponent } from './components/ach-cycle-form.component';
import { NachaExportComponent } from './components/nacha-export.component';
import { permissionGuard } from '../../core/guards/permission.guard';

export const ACH_CYCLES_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    component: AchCycleListComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Ciclos de operación', title: 'Ciclos de operación' }
  },
  {
    path: 'new',
    component: AchCycleFormComponent,
    canActivate: [permissionGuard],
    data: { breadcrumb: 'Nuevo ciclo', title: 'Crear ciclo', permissions: ['CanManageAch'] }
  },
  {
    path: 'nacha/export',
    component: NachaExportComponent,
    canActivate: [permissionGuard],
    data: { breadcrumb: 'Exportar archivo NACHA-M', title: 'Exportar archivo NACHA-M', permissions: ['CanReadAch'] }
  },
  {
    path: 'nacha/layouts',
    pathMatch: 'full',
    redirectTo: '/not-found'
  },
  {
    path: 'nacha/definitions',
    pathMatch: 'full',
    redirectTo: '/not-found'
  },
  {
    path: ':id/edit',
    component: AchCycleFormComponent,
    canActivate: [permissionGuard],
    data: { breadcrumb: 'Editar ciclo', title: 'Editar ciclo', permissions: ['CanManageAch'] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(ACH_CYCLES_ROUTES)],
  exports: [RouterModule]
})
export class AchCyclesRoutingModule {}
