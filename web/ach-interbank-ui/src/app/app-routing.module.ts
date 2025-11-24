import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AppShellComponent } from './layout/app-shell.component';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { permissionGuard } from './core/guards/permission.guard';

const routes: Routes = [
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule)
  },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'transactions',
        canActivate: [roleGuard, permissionGuard],
        data: { roles: ['Admin', 'ACH.Operator'], permissions: ['CanManageAch', 'CanReadAch'] },
        loadChildren: () => import('./features/transactions/transactions.module').then((m) => m.TransactionsModule)
      },
      { path: '', pathMatch: 'full', redirectTo: 'transactions/new' },
      { path: '**', redirectTo: 'transactions/new' }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
