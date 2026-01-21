import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { AchCyclesRoutingModule } from './ach-cycles-routing.module';
import { AchCycleListComponent } from './components/ach-cycle-list.component';
import { AchCycleFormComponent } from './components/ach-cycle-form.component';
import { NachaExportComponent } from './components/nacha-export.component';
import { NachaLayoutsComponent } from './components/nacha-layouts.component';
import { NachaRecordDefinitionsComponent } from './components/nacha-record-definitions.component';

@NgModule({
  imports: [
    SharedModule,
    AchCyclesRoutingModule,
    AchCycleListComponent,
    AchCycleFormComponent,
    NachaExportComponent,
    NachaLayoutsComponent,
    NachaRecordDefinitionsComponent
  ]
})
export class AchCyclesModule {}
