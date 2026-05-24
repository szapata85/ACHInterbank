import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NachaInboundSimulatorComponent } from './components/nacha-inbound-simulator/nacha-inbound-simulator.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'nacha-inbound-simulator' },
  {
    path: 'nacha-inbound-simulator',
    component: NachaInboundSimulatorComponent,
    data: { title: 'Simulador NACHA-M Entrada', breadcrumb: 'Simulador NACHA-M Entrada' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class UatRoutingModule {}
