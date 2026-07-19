import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AliasesListComponent } from './components/aliases-list.component';
import { AliasFormComponent } from './components/alias-form.component';
import { permissionGuard } from '../../core/guards/permission.guard';

export const ALIASES_ROUTES: Routes = [
  {
    path: '',
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAliases'], breadcrumb: 'Alias y cuentas', title: 'Alias y llaves' },
    children: [
      { path: '', component: AliasesListComponent },
      {
        path: 'new',
        component: AliasFormComponent,
        canActivate: [permissionGuard],
        data: { breadcrumb: 'Nuevo alias', title: 'Crear alias', permissions: ['CanManageAliases'] }
      },
      {
        path: ':id/edit',
        component: AliasFormComponent,
        canActivate: [permissionGuard],
        data: { breadcrumb: 'Editar alias', title: 'Editar alias', permissions: ['CanManageAliases'] }
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(ALIASES_ROUTES)],
  exports: [RouterModule]
})
export class AliasesRoutingModule {}
