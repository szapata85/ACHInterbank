import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { TransactionsRoutingModule } from './transactions-routing.module';
import { TransactionCreateComponent } from './components/transaction-create/transaction-create.component';

@NgModule({
  declarations: [TransactionCreateComponent],
  imports: [SharedModule, RouterModule, TransactionsRoutingModule]
})
export class TransactionsModule {}
