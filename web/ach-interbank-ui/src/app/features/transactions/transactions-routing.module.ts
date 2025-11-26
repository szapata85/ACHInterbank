import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CreateTransactionComponent } from './pages/create-transaction/create-transaction.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'create' },
  {
    path: 'create',
    component: CreateTransactionComponent,
    data: { title: 'Crear transacción', breadcrumb: 'Crear transacción' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TransactionsRoutingModule {}
