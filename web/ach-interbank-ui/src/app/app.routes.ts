import { Routes } from '@angular/router';
import { LoginComponent } from './auth/login.component';
import { AppShellComponent } from './layout/app-shell.component';
import { authGuard } from './core/guards/auth.guard';
import { TransactionFormComponent } from './transactions/transaction-form.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'transactions/new', component: TransactionFormComponent },
      { path: '', pathMatch: 'full', redirectTo: 'transactions/new' },
      { path: '**', redirectTo: 'transactions/new' }
    ]
  }
];
