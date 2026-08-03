import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { TransactionsRoutingModule } from './transactions-routing.module';
import { TransactionCreateComponent } from './components/transaction-create/transaction-create.component';
import { TransactionListComponent } from './components/transaction-list/transaction-list.component';
import { NachaUploadComponent } from './components/nacha-upload/nacha-upload.component';
import { AchReturnsManagementComponent } from './components/ach-returns-management/ach-returns-management.component';
import { TransactionBulkCreateComponent } from './components/transaction-bulk-create/transaction-bulk-create.component';
import { BulkIngestionUploadComponent } from './components/bulk-ingestion-upload/bulk-ingestion-upload.component';
import { BulkIngestionTrackingComponent } from './components/bulk-ingestion-tracking/bulk-ingestion-tracking.component';
import { BulkIngestionDetailComponent } from './components/bulk-ingestion-detail/bulk-ingestion-detail.component';
import { CycleConfigManagementComponent } from './components/cycle-config-management/cycle-config-management.component';
import { AchReturnOfReturnManagementComponent } from './components/ach-return-of-return-management/ach-return-of-return-management.component';
import { OutgoingTransactionMonitoringListComponent } from './outgoing-monitoring/outgoing-transaction-monitoring-list.component';
import { OutgoingTransactionMonitoringDetailComponent } from './outgoing-monitoring/outgoing-transaction-monitoring-detail.component';

@NgModule({
  imports: [
    SharedModule,
    RouterModule,
    TransactionCreateComponent,
    TransactionListComponent,
    NachaUploadComponent,
    AchReturnsManagementComponent,
    TransactionBulkCreateComponent,
    BulkIngestionUploadComponent,
    BulkIngestionTrackingComponent,
    BulkIngestionDetailComponent,
    CycleConfigManagementComponent,
    AchReturnOfReturnManagementComponent,
    OutgoingTransactionMonitoringListComponent,
    OutgoingTransactionMonitoringDetailComponent,
    TransactionsRoutingModule
  ]
})
export class TransactionsModule {}
