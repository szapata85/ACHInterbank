import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { CustomerThirdPartiesRoutingModule } from './customer-third-parties-routing.module';
import { CustomerThirdPartiesComponent } from './components/customer-third-parties.component';

@NgModule({
  imports: [SharedModule, CustomerThirdPartiesRoutingModule, CustomerThirdPartiesComponent]
})
export class CustomerThirdPartiesModule {}
