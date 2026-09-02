import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { MainLayoutComponent } from './layout/main-layout.component';
import { LoginLayoutComponent } from './layout/login-layout.component';

export const APP_ROUTES: Routes = [
  {
    path: 'login',
    component: LoginLayoutComponent,
    data: { title: 'Autenticación', breadcrumb: 'Autenticación' },
    children: [
      {
        path: '',
        loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent)
      }
    ]
  },
  {
    path: 'forgot-password',
    component: LoginLayoutComponent,
    data: { title: 'Recuperar acceso', breadcrumb: 'Recuperar acceso' },
    children: [
      {
        path: '',
        loadComponent: () => import('./features/auth/forgot-password.component').then((m) => m.ForgotPasswordComponent)
      }
    ]
  },
  {
    path: 'reset-password',
    component: LoginLayoutComponent,
    data: { title: 'Restablecer contraseña', breadcrumb: 'Restablecer contraseña' },
    children: [
      {
        path: '',
        loadComponent: () => import('./features/auth/reset-password.component').then((m) => m.ResetPasswordComponent)
      }
    ]
  },
  {
    path: 'reset-password/:token',
    component: LoginLayoutComponent,
    data: { title: 'Restablecer contraseña', breadcrumb: 'Restablecer contraseña' },
    children: [
      {
        path: '',
        loadComponent: () => import('./features/auth/reset-password.component').then((m) => m.ResetPasswordComponent)
      }
    ]
  },
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
        path: 'nacha-layouts',
        pathMatch: 'full',
        redirectTo: 'not-found'
      },
      {
        path: 'nacha-record-definitions',
        pathMatch: 'full',
        redirectTo: 'not-found'
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
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch', 'CanManageAch'],
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
        path: 'clearing-houses',
        canActivate: [permissionGuard],
        data: { permissions: ['ClearingHouses.View'], breadcrumb: 'Cámaras compensadoras', title: 'Cámaras compensadoras' },
        children: [
          { path: '', loadComponent: () => import('./features/clearing-houses/clearing-houses.component').then((m) => m.ClearingHousesComponent) },
          {
            path: ':id/cycles',
            canActivate: [permissionGuard],
            data: {
              permissions: ['ClearingHouses.View', 'ClearingHouses.ManageCycles'],
              breadcrumb: 'Ciclos',
              title: 'Configuración de ciclos'
            },
            loadComponent: () =>
              import('./features/transactions/components/cycle-config-management/cycle-config-management.component')
                .then((m) => m.CycleConfigManagementComponent)
          },
          {
            path: ':id/transaction-policies',
            canActivate: [permissionGuard],
            data: {
              permissions: ['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch'],
              breadcrumb: 'Políticas transaccionales',
              title: 'Políticas transaccionales'
            },
            loadComponent: () =>
              import('./features/clearing-houses/transaction-policies.component')
                .then((m) => m.TransactionPoliciesComponent)
          },
          {
            path: ':id/special-dates',
            canActivate: [permissionGuard],
            data: {
              permissions: ['ClearingHouses.View', 'ClearingHouses.ManageSpecialDates'],
              breadcrumb: 'Fechas especiales',
              title: 'Fechas especiales'
            },
            loadComponent: () =>
              import('./features/catalogs/components/clearing-house-special-dates.component')
                .then((m) => m.ClearingHouseSpecialDatesComponent)
          }
        ]
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
        path: 'ach-colombia/file-exchange',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch'],
          breadcrumb: 'Intercambio de archivos ACH Colombia',
          title: 'Intercambio de archivos ACH Colombia'
        },
        loadComponent: () => import('./features/ach-colombia-file-exchange/ach-colombia-file-exchange.component').then((m) => m.AchColombiaFileExchangeComponent)
      },
      {
        path: 'administracion/mft-ach', canActivate: [roleGuard, permissionGuard],
        data: { roles: ['Admin', 'ACH.Operator'], permissions: ['CanReadAch'], breadcrumb: 'MFT ACH', title: 'Administración MFT ACH' },
        loadComponent: () => import('./features/ach-colombia-mft-administration/ach-colombia-mft-administration.component').then((m) => m.AchColombiaMftAdministrationComponent)
      },
      {
        path: 'cenit',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch'],
          breadcrumb: 'CENIT',
          title: 'Centro de operación CENIT'
        },
        loadChildren: () => import('./features/cenit/cenit.module').then((m) => m.CenitModule)
      },
      {
        path: 'nacha-config-admin',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch'],
          breadcrumb: 'Configuración NACHA-M',
          title: 'Administración de perfiles NACHA-M'
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
          breadcrumb: 'Seguimiento de archivos NACHA-M',
          title: 'Seguimiento de archivos NACHA-M'
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
          import('./features/ach-responses/ach-responses.module').then((m) => m.AchResponsesModule)
      },
      {
        path: 'ach',
        canActivate: [roleGuard, permissionGuard],
        data: {
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch'],
          breadcrumb: 'ACH',
          title: 'Consulta operativa NACHA-M'
        },
        loadChildren: () => import('./features/nacha-operational/nacha-operational.module').then((m) => m.NachaOperationalModule)
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
  imports: [RouterModule.forRoot(APP_ROUTES)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
