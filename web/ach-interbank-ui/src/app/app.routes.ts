import { Routes } from '@angular/router';
import { TransactionFormComponent } from './transactions/transaction-form.component';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    component: TransactionFormComponent
  },
  {
    path: '**',
    redirectTo: ''
  }
];
