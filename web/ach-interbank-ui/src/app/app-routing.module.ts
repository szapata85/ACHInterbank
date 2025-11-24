import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { MainLayoutComponent } from './layout/main-layout.component';
import { LoginLayoutComponent } from './layout/login-layout.component';

const routes: Routes = [
  {
    path: 'login',
    component: LoginLayoutComponent,
    data: { title: 'Iniciar sesión', breadcrumb: 'Login' },
    loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule)
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'transactions',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanManageAch', 'CanReadAch'],
          breadcrumb: 'Transacciones',
          title: 'Transacciones ACH'
        },
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
