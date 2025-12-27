import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TransactionCreateComponent } from './components/transaction-create/transaction-create.component';
import { TransactionListComponent } from './components/transaction-list/transaction-list.component';
import { NachaUploadComponent } from './components/nacha-upload/nacha-upload.component';

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
    path: 'nacha-upload',
    component: NachaUploadComponent,
    data: { title: 'Cargar NACHA-M', breadcrumb: 'Cargar NACHA-M' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TransactionsRoutingModule {}
