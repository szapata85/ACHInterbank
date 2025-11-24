import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { UsersListComponent } from './components/users-list.component';
import { UserFormComponent } from './components/user-form.component';
import { UserRolesComponent } from './components/user-roles.component';
import { roleGuard } from '../../core/guards/role.guard';
import { permissionGuard } from '../../core/guards/permission.guard';

const routes: Routes = [
  {
    path: '',
    canActivate: [roleGuard, permissionGuard],
    data: {
      roles: ['Admin'],
      permissions: ['CanManageUsers'],
      breadcrumb: 'Usuarios',
      title: 'Gestión de usuarios'
    },
    children: [
      { path: '', component: UsersListComponent },
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
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule {}
