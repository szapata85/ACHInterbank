import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { MainLayoutComponent } from './layout/main-layout.component';
import { LoginLayoutComponent } from './layout/login-layout.component';

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
      path: 'nacha-security',
      loadChildren: () => import('./features/nacha-security/nacha-security.module').then((m) => m.NachaSecurityModule)
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
        path: 'navigation',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin'],
          permissions: ['CanManageUsers'],
          breadcrumb: 'Navegación',
          title: 'Menú de navegación'
        },
        loadChildren: () => import('./features/navigation/navigation.module').then((m) => m.NavigationModule)
      },
      {
        path: 'catalogs',
        loadChildren: () => import('./features/catalogs/catalogs.module').then((m) => m.CatalogsModule)
      },
      {
        path: 'unauthorized',
        data: { title: 'No autorizado', breadcrumb: 'Error 403' },
        loadComponent: () =>
          import('./shared/components/status/unauthorized.component').then((m) => m.UnauthorizedComponent)
      },
      {
        path: 'not-found',
        data: { title: 'No encontrado', breadcrumb: 'Error 404' },
        loadComponent: () =>
          import('./shared/components/status/not-found.component').then((m) => m.NotFoundComponent)
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
