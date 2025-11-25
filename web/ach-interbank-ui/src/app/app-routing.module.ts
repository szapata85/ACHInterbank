import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { MainLayoutComponent } from './layout/main-layout.component';
import { LoginLayoutComponent } from './layout/login-layout.component';
import { UnauthorizedComponent } from './shared/components/status/unauthorized.component';
import { NotFoundComponent } from './shared/components/status/not-found.component';

const routes: Routes = [
  {
    path: 'auth',
    component: LoginLayoutComponent,
    data: { title: 'Autenticación', breadcrumb: 'Auth' },
    loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule)
  },
  { path: 'login', pathMatch: 'full', redirectTo: '/auth/login' },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.module').then((m) => m.DashboardModule)
      },
      {
        path: 'users',
        loadChildren: () => import('./features/admin/admin.module').then((m) => m.AdminModule)
      },
      {
        path: 'aliases',
        loadChildren: () => import('./features/aliases/aliases.module').then((m) => m.AliasesModule)
      },
      {
        path: 'ach-cycles',
        loadChildren: () => import('./features/ach-cycles/ach-cycles.module').then((m) => m.AchCyclesModule)
      },
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
      {
        path: 'catalogs',
        loadChildren: () => import('./features/catalogs/catalogs.module').then((m) => m.CatalogsModule)
      },
      {
        path: 'unauthorized',
        component: UnauthorizedComponent,
        data: { title: 'No autorizado', breadcrumb: 'Error 403' }
      },
      {
        path: 'not-found',
        component: NotFoundComponent,
        data: { title: 'No encontrado', breadcrumb: 'Error 404' }
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: '**', redirectTo: 'not-found' }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
