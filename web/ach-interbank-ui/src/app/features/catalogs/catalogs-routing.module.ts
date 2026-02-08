import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CatalogsListComponent } from './components/catalogs-list.component';
import { permissionGuard } from '../../core/guards/permission.guard';

const routes: Routes = [
  {
    path: '',
    component: CatalogsListComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadCatalogs'], breadcrumb: 'Catálogos', title: 'Catálogos' }
  },
  {
    path: 'financial-institutions',
    loadComponent: () =>
      import('./components/financial-institutions.component').then((m) => m.FinancialInstitutionsComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
      breadcrumb: 'Instituciones financieras',
      title: 'Instituciones financieras'
    }
  },
  {
    path: 'clearing-house-preferences',
    loadComponent: () =>
      import('./components/clearing-house-preferences.component').then((m) => m.ClearingHousePreferencesComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
      breadcrumb: 'Prioridades cámaras',
      title: 'Prioridades cámaras'
    }
  },
  {
    path: 'bank-holidays',
    loadComponent: () => import('./components/bank-holidays.component').then((m) => m.BankHolidaysComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
      breadcrumb: 'Festivos bancarios',
      title: 'Festivos bancarios'
    }
  },
  {
    path: 'clearing-house-special-dates',
    loadComponent: () =>
      import('./components/clearing-house-special-dates.component').then((m) => m.ClearingHouseSpecialDatesComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
      breadcrumb: 'Fechas especiales cámaras',
      title: 'Fechas especiales cámaras'
    }
  },
  {
    path: 'document-types',
    loadComponent: () =>
      import('./components/catalog-types-admin.component').then((m) => m.CatalogTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
      breadcrumb: 'Tipos de documento',
      title: 'Tipos de documento',
      subtitle: 'Administra el catálogo de tipos de documento.',
      catalogType: 'document-types'
    }
  },
  {
    path: 'gender-types',
    loadComponent: () => import('./components/catalog-types-admin.component').then((m) => m.CatalogTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
      breadcrumb: 'Tipos de género',
      title: 'Tipos de género',
      subtitle: 'Administra el catálogo de tipos de género.',
      catalogType: 'gender-types'
    }
  },
  {
    path: 'person-types',
    loadComponent: () => import('./components/catalog-types-admin.component').then((m) => m.CatalogTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
      breadcrumb: 'Tipos de persona',
      title: 'Tipos de persona',
      subtitle: 'Administra el catálogo de tipos de persona.',
      catalogType: 'person-types'
    }
  },
  {
    path: 'phone-types',
    loadComponent: () => import('./components/catalog-types-admin.component').then((m) => m.CatalogTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
      breadcrumb: 'Tipos de teléfono',
      title: 'Tipos de teléfono',
      subtitle: 'Administra el catálogo de tipos de teléfono.',
      catalogType: 'phone-types'
    }
  },
  {
    path: 'email-types',
    loadComponent: () => import('./components/catalog-types-admin.component').then((m) => m.CatalogTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
      breadcrumb: 'Tipos de correo',
      title: 'Tipos de correo',
      subtitle: 'Administra el catálogo de tipos de correo.',
      catalogType: 'email-types'
    }
  },
  {
    path: 'address-types',
    loadComponent: () =>
      import('./components/catalog-types-admin.component').then((m) => m.CatalogTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
      breadcrumb: 'Tipos de dirección',
      title: 'Tipos de dirección',
      subtitle: 'Administra el catálogo de tipos de dirección.',
      catalogType: 'address-types'
    }
  },
  {
    path: 'transaction-codes',
    loadComponent: () =>
      import('./components/catalog-types-admin.component').then((m) => m.CatalogTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanManageAch'],
      breadcrumb: 'Códigos de transacción ACH',
      title: 'Códigos de transacción ACH',
      subtitle: 'Administra el catálogo de códigos de transacción ACH.',
      catalogType: 'transaction-codes'
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CatalogsRoutingModule {}
