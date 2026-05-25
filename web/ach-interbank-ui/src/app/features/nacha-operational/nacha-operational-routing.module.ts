import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { NachaOperationalDashboardComponent } from './pages/nacha-operational-dashboard.component';

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
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NachaOperationalRoutingModule {}
