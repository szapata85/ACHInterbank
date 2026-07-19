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
