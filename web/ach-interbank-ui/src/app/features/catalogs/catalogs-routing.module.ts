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
    path: 'error-codes',
    loadComponent: () =>
      import('./components/ach-error-codes.component').then((m) => m.AchErrorCodesComponent),
    canActivate: [permissionGuard],
    data: {
      permissions: ['CanReadCatalogs'],
      breadcrumb: 'Causales de devolución',
      title: 'Causales de devolución ACH'
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CatalogsRoutingModule {}
