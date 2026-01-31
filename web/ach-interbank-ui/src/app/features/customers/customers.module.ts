import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { CustomersRoutingModule } from './customers-routing.module';
import { CustomersListComponent } from './components/customers-list.component';
import { CustomerFormComponent } from './components/customer-form.component';

@NgModule({
  imports: [SharedModule, CustomersRoutingModule, CustomersListComponent, CustomerFormComponent]
})
export class CustomersModule {}
