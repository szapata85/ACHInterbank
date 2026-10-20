import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthLogComponent } from './components/auth-log.component';

const routes: Routes = [
  {
    path: '',
    component: AuthLogComponent,
    data: {
      breadcrumb: 'Registro de accesos',
      title: 'Registro de accesos'
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AuthLogsRoutingModule {}
