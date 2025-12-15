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
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CatalogsRoutingModule {}
