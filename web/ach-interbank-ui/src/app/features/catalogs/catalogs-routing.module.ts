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
      import('./components/document-types-admin.component').then((m) => m.DocumentTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadCatalogs'],
      breadcrumb: 'Tipos de documento',
      title: 'Tipos de documento'
    }
  },
  {
    path: 'gender-types',
    loadComponent: () => import('./components/gender-types-admin.component').then((m) => m.GenderTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadCatalogs'],
      breadcrumb: 'Tipos de género',
      title: 'Tipos de género'
    }
  },
  {
    path: 'person-types',
    loadComponent: () => import('./components/person-types-admin.component').then((m) => m.PersonTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadCatalogs'],
      breadcrumb: 'Tipos de persona',
      title: 'Tipos de persona'
    }
  },
  {
    path: 'phone-types',
    loadComponent: () => import('./components/phone-types-admin.component').then((m) => m.PhoneTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadCatalogs'],
      breadcrumb: 'Tipos de teléfono',
      title: 'Tipos de teléfono'
    }
  },
  {
    path: 'email-types',
    loadComponent: () => import('./components/email-types-admin.component').then((m) => m.EmailTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadCatalogs'],
      breadcrumb: 'Tipos de correo',
      title: 'Tipos de correo'
    }
  },
  {
    path: 'address-types',
    loadComponent: () =>
      import('./components/address-types-admin.component').then((m) => m.AddressTypesAdminComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadCatalogs'],
      breadcrumb: 'Tipos de dirección',
      title: 'Tipos de dirección'
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CatalogsRoutingModule {}
