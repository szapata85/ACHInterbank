import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AchCycleListComponent } from './components/ach-cycle-list.component';
import { AchCycleFormComponent } from './components/ach-cycle-form.component';
import { NachaExportComponent } from './components/nacha-export.component';
import { permissionGuard } from '../../core/guards/permission.guard';

const routes: Routes = [
  {
    path: '',
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Ciclos ACH', title: 'Ciclos ACH' },
    children: [
      { path: '', component: AchCycleListComponent },
      {
        path: 'new',
        component: AchCycleFormComponent,
        data: { breadcrumb: 'Nuevo ciclo', title: 'Crear ciclo', permissions: ['CanManageAch'] }
      },
      {
        path: 'nacha/export',
        component: NachaExportComponent,
        data: { breadcrumb: 'Exportar NACHA', title: 'Exportar NACHA-M', permissions: ['CanReadAch'] }
      },
      {
        path: ':id/edit',
        component: AchCycleFormComponent,
        data: { breadcrumb: 'Editar ciclo', title: 'Editar ciclo', permissions: ['CanManageAch'] }
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AchCyclesRoutingModule {}
