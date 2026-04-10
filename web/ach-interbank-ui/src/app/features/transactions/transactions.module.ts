import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { TransactionsRoutingModule } from './transactions-routing.module';
import { TransactionCreateComponent } from './components/transaction-create/transaction-create.component';
import { TransactionListComponent } from './components/transaction-list/transaction-list.component';
import { NachaUploadComponent } from './components/nacha-upload/nacha-upload.component';
import { AchReturnsManagementComponent } from './components/ach-returns-management/ach-returns-management.component';
import { TransactionBulkCreateComponent } from './components/transaction-bulk-create/transaction-bulk-create.component';

@NgModule({
  imports: [
    SharedModule,
    RouterModule,
    TransactionCreateComponent,
    TransactionListComponent,
    NachaUploadComponent,
    AchReturnsManagementComponent,
    TransactionBulkCreateComponent,
    TransactionsRoutingModule
  ]
})
export class TransactionsModule {}
