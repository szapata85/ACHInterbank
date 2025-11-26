import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { CreateTransactionComponent } from './pages/create-transaction/create-transaction.component';
import { TransactionsRoutingModule } from './transactions-routing.module';

@NgModule({
  imports: [SharedModule, RouterModule, CreateTransactionComponent, TransactionsRoutingModule]
})
export class TransactionsModule {}
