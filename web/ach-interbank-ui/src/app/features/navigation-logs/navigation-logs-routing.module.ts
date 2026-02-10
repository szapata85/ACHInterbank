import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NavigationLogComponent } from './components/navigation-log.component';

const routes: Routes = [
  {
    path: '',
    component: NavigationLogComponent,
    data: { breadcrumb: 'Log de navegación', title: 'Log de navegación' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NavigationLogsRoutingModule {}
