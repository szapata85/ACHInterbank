import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { TransactionsRoutingModule } from './transactions-routing.module';
import { TransactionCreateComponent } from './components/transaction-create/transaction-create.component';
import { TransactionListComponent } from './components/transaction-list/transaction-list.component';
import { NachaUploadComponent } from './components/nacha-upload/nacha-upload.component';

@NgModule({
  imports: [
    SharedModule,
    RouterModule,
    TransactionCreateComponent,
    TransactionListComponent,
    NachaUploadComponent,
    TransactionsRoutingModule
  ]
})
export class TransactionsModule {}
