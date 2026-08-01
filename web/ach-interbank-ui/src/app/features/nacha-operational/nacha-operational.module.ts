import { NgModule } from '@angular/core';
import { NachaOperationalRoutingModule } from './nacha-operational-routing.module';
import { AchReconciliationConsoleComponent } from './pages/ach-reconciliation-console.component';

@NgModule({
  imports: [NachaOperationalRoutingModule, AchReconciliationConsoleComponent]
})
export class NachaOperationalModule {}
