import { NgModule } from '@angular/core';
import { PaymentRailCapabilityRegistryPageComponent } from './pages/payment-rail-capability-registry-page.component';
import { PaymentRailCapabilityRegistryRoutingModule } from './payment-rail-capability-registry-routing.module';

@NgModule({
  imports: [PaymentRailCapabilityRegistryRoutingModule, PaymentRailCapabilityRegistryPageComponent]
})
export class PaymentRailCapabilityRegistryModule {}
