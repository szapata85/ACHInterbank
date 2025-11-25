import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { TransactionsRoutingModule } from './transactions-routing.module';
import { TransactionCreateComponent } from './components/transaction-create/transaction-create.component';

@NgModule({
  imports: [SharedModule, TransactionsRoutingModule, TransactionCreateComponent]
})
export class TransactionsModule {}
