import { NgModule } from '@angular/core';
import { NachaOperationalRoutingModule } from './nacha-operational-routing.module';
import { NachaOperationalDashboardComponent } from './pages/nacha-operational-dashboard.component';
import { AchReconciliationConsoleComponent } from './pages/ach-reconciliation-console.component';

@NgModule({
  imports: [NachaOperationalRoutingModule, NachaOperationalDashboardComponent, AchReconciliationConsoleComponent]
})
export class NachaOperationalModule {}
