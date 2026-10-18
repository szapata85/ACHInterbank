import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CustomersListComponent } from './components/customers-list.component';
import { CustomerFormComponent } from './components/customer-form.component';
import { permissionGuard } from '../../core/guards/permission.guard';

export const CUSTOMERS_ROUTES: Routes = [
  {
    path: '',
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], breadcrumb: 'Clientes', title: 'Clientes' },
    children: [
      { path: '', component: CustomersListComponent },
      {
        path: 'new',
        component: CustomerFormComponent,
        canActivate: [permissionGuard],
        data: { breadcrumb: 'Nuevo cliente', title: 'Crear cliente', permissions: ['CanManageAch'] }
      },
      {
        path: ':id/third-parties',
        canActivate: [permissionGuard],
        data: { breadcrumb: 'Terceros', title: 'Terceros del cliente', permissions: ['CanReadAch'] },
        loadComponent: () =>
          import('./third-party-administration/customer-third-parties-admin.component')
            .then((m) => m.CustomerThirdPartiesAdminComponent)
      },
      {
        path: ':id/third-parties/:thirdPartyId',
        canActivate: [permissionGuard],
        data: { breadcrumb: 'Detalle del tercero', title: 'Detalle del tercero', permissions: ['CanReadAch'] },
        loadComponent: () =>
          import('./third-party-administration/third-party-admin-detail.component')
            .then((m) => m.ThirdPartyAdminDetailComponent)
      },
      {
        path: ':id/edit',
        component: CustomerFormComponent,
        canActivate: [permissionGuard],
        data: { breadcrumb: 'Editar cliente', title: 'Editar cliente', permissions: ['CanManageAch'] }
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(CUSTOMERS_ROUTES)],
  exports: [RouterModule]
})
export class CustomersRoutingModule {}
