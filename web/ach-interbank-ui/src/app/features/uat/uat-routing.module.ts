import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NachaInboundSimulatorComponent } from './components/nacha-inbound-simulator/nacha-inbound-simulator.component';
import { permissionGuard } from '../../core/guards/permission.guard';

export const UAT_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'nacha-inbound-simulator' },
  {
    path: 'nacha-inbound-simulator',
    component: NachaInboundSimulatorComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
      title: 'Simulador NACHA-M de entrada',
      breadcrumb: 'Simulador NACHA-M de entrada'
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(UAT_ROUTES)],
  exports: [RouterModule]
})
export class UatRoutingModule {}
