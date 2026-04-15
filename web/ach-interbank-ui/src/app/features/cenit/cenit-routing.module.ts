import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CenitHomeComponent } from './components/cenit-home.component';
import { CenitOperationPageComponent } from './components/cenit-operation-page.component';
import { CenitRegulatoryPageComponent } from './components/cenit-regulatory-page.component';

const routes: Routes = [
  { path: '', component: CenitHomeComponent },
  { path: 'regulatorio/causales-devolucion', component: CenitRegulatoryPageComponent, data: { view: 'causales-devolucion' } },
  { path: 'regulatorio/causales-rechazo', component: CenitRegulatoryPageComponent, data: { view: 'causales-rechazo' } },
  { path: 'regulatorio/politicas-transaccion', component: CenitRegulatoryPageComponent, data: { view: 'politicas-transaccion' } },
  { path: 'regulatorio/politicas-devolucion', component: CenitRegulatoryPageComponent, data: { view: 'politicas-devolucion' } },
  { path: 'regulatorio/politicas-prenotificacion', component: CenitRegulatoryPageComponent, data: { view: 'politicas-prenotificacion' } },
  { path: 'operacion/ciclos', component: CenitOperationPageComponent, data: { view: 'ciclos' } },
  { path: 'operacion/cola', component: CenitOperationPageComponent, data: { view: 'cola' } },
  { path: 'operacion/neteo', component: CenitOperationPageComponent, data: { view: 'neteo' } },
  { path: 'operacion/optimizacion', component: CenitOperationPageComponent, data: { view: 'optimizacion' } },
  { path: 'operacion/devoluciones', component: CenitOperationPageComponent, data: { view: 'devoluciones' } },
  { path: 'operacion/trazabilidad', component: CenitOperationPageComponent, data: { view: 'trazabilidad' } }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CenitRoutingModule {}
