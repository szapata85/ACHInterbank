import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PaymentRailCapabilityRegistryPageComponent } from './pages/payment-rail-capability-registry-page.component';

const routes: Routes = [{ path: '', component: PaymentRailCapabilityRegistryPageComponent }];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PaymentRailCapabilityRegistryRoutingModule {}
