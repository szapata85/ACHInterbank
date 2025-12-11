import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { TransactionsRoutingModule } from './transactions-routing.module';
import { TransactionCreateComponent } from './components/transaction-create/transaction-create.component';
import { TransactionListComponent } from './components/transaction-list/transaction-list.component';

@NgModule({
  imports: [SharedModule, RouterModule, TransactionCreateComponent, TransactionListComponent, TransactionsRoutingModule]
})
export class TransactionsModule {}
