import { NgModule } from '@angular/core';
import { NachaOperationalRoutingModule } from './nacha-operational-routing.module';
import { NachaOperationalDashboardComponent } from './pages/nacha-operational-dashboard.component';

@NgModule({
  imports: [NachaOperationalRoutingModule, NachaOperationalDashboardComponent]
})
export class NachaOperationalModule {}
