import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { UatRoutingModule } from './uat-routing.module';
import { NachaInboundSimulatorComponent } from './components/nacha-inbound-simulator/nacha-inbound-simulator.component';

@NgModule({
  imports: [SharedModule, NachaInboundSimulatorComponent, UatRoutingModule]
})
export class UatModule {}
