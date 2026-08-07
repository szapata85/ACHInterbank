import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { UsersListComponent } from './components/users-list.component';
import { UserFormComponent } from './components/user-form.component';
import { UserRolesComponent } from './components/user-roles.component';
import { roleGuard } from '../../core/guards/role.guard';
import { permissionGuard } from '../../core/guards/permission.guard';
import { BrandingSettingsComponent } from './components/branding-settings.component';
import { PasswordRulesSettingsComponent } from './components/password-rules-settings.component';
import { LoginLockoutSettingsComponent } from './components/login-lockout-settings.component';
import { pendingUserChangesGuard } from './guards/pending-user-changes.guard';

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
        data: { breadcrumb: 'Usuarios', title: 'Administración de usuarios' }
      },
      {
        path: 'list',
        component: UsersListComponent,
        data: { breadcrumb: 'Administrar usuarios', title: 'Administración de usuarios' }
      },
      { path: 'new', component: UserFormComponent, canDeactivate: [pendingUserChangesGuard], data: { breadcrumb: 'Nuevo usuario', title: 'Crear usuario' } },
      {
        path: ':id/edit',
        component: UserFormComponent,
        canDeactivate: [pendingUserChangesGuard],
        data: { breadcrumb: 'Editar usuario', title: 'Editar usuario' }
      },
      {
        path: ':id/roles',
        component: UserRolesComponent,
        data: { breadcrumb: 'Perfiles de acceso', title: 'Administrar perfiles de acceso' }
      },
      {
        path: 'branding',
        component: BrandingSettingsComponent,
        data: { breadcrumb: 'Identidad visual', title: 'Identidad visual' }
      },
      {
        path: 'password-rules',
        component: PasswordRulesSettingsComponent,
        data: { breadcrumb: 'Reglas de contraseña', title: 'Reglas de contraseña' }
      },
      {
        path: 'login-lockout',
        component: LoginLockoutSettingsComponent,
        data: { breadcrumb: 'Bloqueo de acceso', title: 'Bloqueo de acceso' }
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule {}
