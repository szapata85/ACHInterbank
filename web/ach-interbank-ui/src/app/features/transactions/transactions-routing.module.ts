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
import { AchReturnOfReturnManagementComponent } from './components/ach-return-of-return-management/ach-return-of-return-management.component';
import { permissionGuard } from '../../core/guards/permission.guard';
import { OutgoingTransactionMonitoringListComponent } from './outgoing-monitoring/outgoing-transaction-monitoring-list.component';
import { OutgoingTransactionMonitoringDetailComponent } from './outgoing-monitoring/outgoing-transaction-monitoring-detail.component';

export const TRANSACTIONS_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'list' },
  {
    path: 'outgoing-monitoring',
    component: OutgoingTransactionMonitoringListComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['OutgoingTransactions.Monitor.Read', 'CanReadAch', 'CanManageAch'],
      title: 'Transacciones enviadas',
      breadcrumb: 'Transacciones enviadas'
    }
  },
  {
    path: 'outgoing-monitoring/:id',
    component: OutgoingTransactionMonitoringDetailComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['OutgoingTransactions.Monitor.Read', 'CanReadAch', 'CanManageAch'],
      title: 'Detalle de transacción de salida',
      breadcrumb: 'Detalle de salida'
    }
  },
  {
    path: 'list',
    component: TransactionListComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], title: 'Consultar transacciones', breadcrumb: 'Consultar transacciones' }
  },
  {
    path: 'create',
    component: TransactionCreateComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['Transactions.Create', 'CanManageAch'], title: 'Crear transacción', breadcrumb: 'Crear transacción' }
  },
  {
    path: 'bulk-create',
    component: TransactionBulkCreateComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanManageAch'], title: 'Crear transacción masiva', breadcrumb: 'Crear transacción masiva' }
  },

  {
    path: 'bulk-ingestion/upload',
    component: BulkIngestionUploadComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['BulkIngestion.Upload', 'CanManageAch'], title: 'Cargar transacciones desde archivo', breadcrumb: 'Cargar transacciones desde archivo' }
  },
  {
    path: 'bulk-ingestion/tracking',
    component: BulkIngestionTrackingComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['BulkIngestion.Read', 'CanReadAch'], title: 'Seguimiento de cargas', breadcrumb: 'Seguimiento de cargas' }
  },
  {
    path: 'bulk-ingestion/:batchId',
    component: BulkIngestionDetailComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['BulkIngestion.Read', 'CanReadAch'], title: 'Detalle de lote', breadcrumb: 'Detalle de lote' }
  },
  {
    path: 'nacha-upload',
    component: NachaUploadComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['Nacha.Upload', 'CanManageAch'], title: 'Cargar archivo NACHA-M', breadcrumb: 'Cargar archivo NACHA-M' }
  },

  {
    path: 'cycle-configs',
    pathMatch: 'full',
    redirectTo: '/clearing-houses',
    canActivate: [permissionGuard],
    data: { permissions: ['CanManageAch'], title: 'Configuración de ciclos', breadcrumb: 'Configuración de ciclos' }
  },
  { path: 'clearing-house-rules', pathMatch: 'full', redirectTo: '/clearing-houses' },
  {
    path: 'returns',
    component: AchReturnsManagementComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], title: 'Devoluciones', breadcrumb: 'Devoluciones' }
  },
  {
    path: 'returns-ror',
    component: AchReturnOfReturnManagementComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['Returns.Read', 'CanReadAch'], title: 'Devolución de una devolución', breadcrumb: 'Devolución de una devolución' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(TRANSACTIONS_ROUTES)],
  exports: [RouterModule]
})
export class TransactionsRoutingModule {}
