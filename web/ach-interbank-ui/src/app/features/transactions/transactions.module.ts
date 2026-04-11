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
    TransactionsRoutingModule
  ]
})
export class TransactionsModule {}
