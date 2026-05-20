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
        path: 'customers',
        loadChildren: () => import('./features/customers/customers.module').then((m) => m.CustomersModule)
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
        path: 'integraciones',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin'],
          permissions: ['CanManageUsers'],
          breadcrumb: 'Integraciones',
          title: 'Integraciones'
        },
        loadChildren: () => import('./features/integrations/integrations.module').then((m) => m.IntegrationsModule)
      },
      {
        path: 'soap-integrations',
        redirectTo: 'integraciones/soap-settings',
        pathMatch: 'full'
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
        path: 'scheduler',
        loadChildren: () => import('./features/scheduler/scheduler.module').then((m) => m.SchedulerModule)
      },
      {
        path: 'log',
        pathMatch: 'full',
        redirectTo: 'audit-logs'
      },
      {
        path: 'logs',
        pathMatch: 'full',
        redirectTo: 'audit-logs'
      },
      {
        path: 'audit-logs',
        loadChildren: () => import('./features/audit/audit.module').then((m) => m.AuditModule)
      },
      {
        path: 'auth-logs',
        loadChildren: () => import('./features/auth-logs/auth-logs.module').then((m) => m.AuthLogsModule)
      },

      {
        path: 'navigation-logs',
        loadChildren: () => import('./features/navigation-logs/navigation-logs.module').then((m) => m.NavigationLogsModule)
      },
      {
        path: 'customer-third-parties',
        loadChildren: () =>
          import('./features/customer-third-parties/customer-third-parties.module').then((m) => m.CustomerThirdPartiesModule)
      },
      {
        path: 'reports',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch'],
          breadcrumb: 'Reportes',
          title: 'Reportes ACH'
        },
        loadChildren: () => import('./features/reports/reports.module').then((m) => m.ReportsModule)
      },
      {
        path: 'cenit',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch'],
          breadcrumb: 'CENIT',
          title: 'Backoffice CENIT'
        },
        loadChildren: () => import('./features/cenit/cenit.module').then((m) => m.CenitModule)
      },
      {
        path: 'nacha-config-admin',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch', 'CanManageAch'],
          breadcrumb: 'NACHA Config',
          title: 'Administración NACHA Config'
        },
        loadChildren: () => import('./features/nacha-config-admin/nacha-config-admin.module').then((m) => m.NachaConfigAdminModule)
      },
      {
        path: 'uat',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanManageAch', 'CanReadAch'],
          breadcrumb: 'UAT',
          title: 'Simuladores UAT'
        },
        loadChildren: () => import('./features/uat/uat.module').then((m) => m.UatModule)
      },
      {
        path: 'incoming-nacha-command-center',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch'],
          breadcrumb: 'Inbound NACHA',
          title: 'Command Center inbound NACHA'
        },
        loadChildren: () =>
          import('./features/incoming-nacha-command-center/incoming-nacha-command-center.module').then(
            (m) => m.IncomingNachaCommandCenterModule
          )
      },
      {
        path: 'ach-responses',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch'],
          breadcrumb: 'Respuestas ACH',
          title: 'Command Center Respuestas ACH'
        },
        loadChildren: () =>
          import('./features/ach-responses/ach-responses.module').then(
            (m) => m.AchResponsesModule
          )
      },
      {
        path: 'payment-rail-capability-registry',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanViewPaymentRailCapabilityRegistry', 'CanManageAch', 'CanReadAch'],
          breadcrumb: 'Capability Registry',
          title: 'Capability Registry multi-riel (solo lectura)'
        },
        loadChildren: () =>
          import('./features/payment-rail-capability-registry/payment-rail-capability-registry.module').then(
            (m) => m.PaymentRailCapabilityRegistryModule
          )
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
