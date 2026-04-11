import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TransactionCreateComponent } from './components/transaction-create/transaction-create.component';
import { TransactionListComponent } from './components/transaction-list/transaction-list.component';
import { NachaUploadComponent } from './components/nacha-upload/nacha-upload.component';
import { AchReturnsManagementComponent } from './components/ach-returns-management/ach-returns-management.component';
import { TransactionBulkCreateComponent } from './components/transaction-bulk-create/transaction-bulk-create.component';
import { BulkIngestionUploadComponent } from './components/bulk-ingestion-upload/bulk-ingestion-upload.component';
import { BulkIngestionTrackingComponent } from './components/bulk-ingestion-tracking/bulk-ingestion-tracking.component';
import { BulkIngestionDetailComponent } from './components/bulk-ingestion-detail/bulk-ingestion-detail.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'list' },
  {
    path: 'list',
    component: TransactionListComponent,
    data: { title: 'Transacciones', breadcrumb: 'Transacciones' }
  },
  {
    path: 'create',
    component: TransactionCreateComponent,
    data: { title: 'Crear transacción', breadcrumb: 'Crear transacción' }
  },
  {
    path: 'bulk-create',
    component: TransactionBulkCreateComponent,
    data: { title: 'Crear transacción masiva', breadcrumb: 'Crear transacción masiva' }
  },

  {
    path: 'bulk-ingestion/upload',
    component: BulkIngestionUploadComponent,
    data: { title: 'Carga masiva por archivo', breadcrumb: 'Carga masiva por archivo' }
  },
  {
    path: 'bulk-ingestion/tracking',
    component: BulkIngestionTrackingComponent,
    data: { title: 'Seguimiento de lotes', breadcrumb: 'Seguimiento de lotes' }
  },
  {
    path: 'bulk-ingestion/:batchId',
    component: BulkIngestionDetailComponent,
    data: { title: 'Detalle de lote', breadcrumb: 'Detalle de lote' }
  },
  {
    path: 'nacha-upload',
    component: NachaUploadComponent,
    data: { title: 'Cargar NACHA-M', breadcrumb: 'Cargar NACHA-M' }
  },
  {
    path: 'returns',
    component: AchReturnsManagementComponent,
    data: { title: 'Devoluciones ACH', breadcrumb: 'Devoluciones ACH' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TransactionsRoutingModule {}
