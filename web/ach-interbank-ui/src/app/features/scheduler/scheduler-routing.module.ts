import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TaskDefinitionsComponent } from './components/task-definitions.component';
import { permissionGuard } from '../../core/guards/permission.guard';

const routes: Routes = [
  {
    path: 'tasks',
    component: TaskDefinitionsComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanManageAch'], breadcrumb: 'Tareas programadas', title: 'TaskDefinitions' }
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'tasks'
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SchedulerRoutingModule {}
