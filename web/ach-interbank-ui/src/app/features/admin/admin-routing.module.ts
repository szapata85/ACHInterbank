import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { UsersListComponent } from './components/users-list.component';
import { UserFormComponent } from './components/user-form.component';
import { UserRolesComponent } from './components/user-roles.component';
import { roleGuard } from '../../core/guards/role.guard';
import { permissionGuard } from '../../core/guards/permission.guard';
import { BrandingSettingsComponent } from './components/branding-settings.component';

const routes: Routes = [
  {
    path: '',
    canActivate: [roleGuard, permissionGuard],
    data: {
      roles: ['Admin'],
      permissions: ['CanManageUsers']
    },
    children: [
      {
        path: '',
        component: UsersListComponent,
        data: { breadcrumb: 'Usuarios', title: 'Gestión de usuarios' }
      },
      { path: 'new', component: UserFormComponent, data: { breadcrumb: 'Nuevo usuario', title: 'Crear usuario' } },
      {
        path: ':id/edit',
        component: UserFormComponent,
        data: { breadcrumb: 'Editar usuario', title: 'Editar usuario' }
      },
      {
        path: ':id/roles',
        component: UserRolesComponent,
        data: { breadcrumb: 'Roles y permisos', title: 'Asignar roles' }
      },
      {
        path: 'branding',
        component: BrandingSettingsComponent,
        data: { breadcrumb: 'Identidad visual', title: 'Identidad y colores' }
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule {}
