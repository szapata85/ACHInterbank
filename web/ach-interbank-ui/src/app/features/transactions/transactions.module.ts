import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { TransactionsRoutingModule } from './transactions-routing.module';
import { TransactionCreateComponent } from './components/transaction-create/transaction-create.component';

@NgModule({
  declarations: [TransactionCreateComponent],
  imports: [SharedModule, TransactionsRoutingModule]
})
export class TransactionsModule {}
