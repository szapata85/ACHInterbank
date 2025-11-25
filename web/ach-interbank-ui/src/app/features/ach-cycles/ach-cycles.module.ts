import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { AchCyclesRoutingModule } from './ach-cycles-routing.module';
import { AchCycleListComponent } from './components/ach-cycle-list.component';
import { AchCycleFormComponent } from './components/ach-cycle-form.component';

@NgModule({
  imports: [SharedModule, AchCyclesRoutingModule, AchCycleListComponent, AchCycleFormComponent]
})
export class AchCyclesModule {}
