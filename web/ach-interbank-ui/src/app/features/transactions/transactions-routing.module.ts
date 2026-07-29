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
import { CycleConfigManagementComponent } from './components/cycle-config-management/cycle-config-management.component';
import { AchReturnOfReturnManagementComponent } from './components/ach-return-of-return-management/ach-return-of-return-management.component';
import { permissionGuard } from '../../core/guards/permission.guard';

export const TRANSACTIONS_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'list' },
  {
    path: 'list',
    component: TransactionListComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], title: 'Transacciones', breadcrumb: 'Transacciones' }
  },
  {
    path: 'create',
    component: TransactionCreateComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanManageAch'], title: 'Crear transacción', breadcrumb: 'Crear transacción' }
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
    data: { permissions: ['CanManageAch'], title: 'Carga masiva por archivo', breadcrumb: 'Carga masiva por archivo' }
  },
  {
    path: 'bulk-ingestion/tracking',
    component: BulkIngestionTrackingComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanManageAch'], title: 'Seguimiento de lotes', breadcrumb: 'Seguimiento de lotes' }
  },
  {
    path: 'bulk-ingestion/:batchId',
    component: BulkIngestionDetailComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanManageAch'], title: 'Detalle de lote', breadcrumb: 'Detalle de lote' }
  },
  {
    path: 'nacha-upload',
    component: NachaUploadComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanManageAch'], title: 'Cargar NACHA-M', breadcrumb: 'Cargar NACHA-M' }
  },

  {
    path: 'cycle-configs',
    component: CycleConfigManagementComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanManageAch'], title: 'Configuración de ciclos', breadcrumb: 'Configuración de ciclos' }
  },
  { path: 'clearing-house-rules', pathMatch: 'full', redirectTo: '/clearing-houses' },
  {
    path: 'returns',
    component: AchReturnsManagementComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanManageAch'], title: 'Devoluciones ACH', breadcrumb: 'Devoluciones ACH' }
  },
  {
    path: 'returns-ror',
    component: AchReturnOfReturnManagementComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanManageAch'], title: 'Devolución de devolución', breadcrumb: 'Devolución de devolución' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(TRANSACTIONS_ROUTES)],
  exports: [RouterModule]
})
export class TransactionsRoutingModule {}
