import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TaskDefinitionsComponent } from './components/task-definitions.component';
import { permissionGuard } from '../../core/guards/permission.guard';

export const SCHEDULER_ROUTES: Routes = [
  {
    path: 'tasks',
    component: TaskDefinitionsComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['Scheduler.View'],
      breadcrumb: 'Tareas programadas',
      title: 'Administración de tareas programadas'
    }
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'tasks'
  }
];

@NgModule({
  imports: [RouterModule.forChild(SCHEDULER_ROUTES)],
  exports: [RouterModule]
})
export class SchedulerRoutingModule {}
