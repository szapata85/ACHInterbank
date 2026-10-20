import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuditLogComponent } from './components/audit-log.component';

const routes: Routes = [
  {
    path: '',
    component: AuditLogComponent,
    data: { breadcrumb: 'Registro de auditoría', title: 'Registro de auditoría' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AuditRoutingModule {}
