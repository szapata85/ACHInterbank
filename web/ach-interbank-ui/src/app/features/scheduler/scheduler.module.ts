import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { SchedulerRoutingModule } from './scheduler-routing.module';
import { TaskDefinitionsComponent } from './components/task-definitions.component';

@NgModule({
  imports: [SharedModule, SchedulerRoutingModule, TaskDefinitionsComponent]
})
export class SchedulerModule {}
