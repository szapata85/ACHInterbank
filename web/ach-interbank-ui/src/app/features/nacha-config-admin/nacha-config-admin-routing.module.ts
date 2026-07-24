import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { roleGuard } from '../../core/guards/role.guard';
import { NachaConfigRecordsPageComponent } from './pages/nacha-config-records-page.component';
import { NachaConfigProfileWorkspacePageComponent } from './pages/nacha-config-profile-workspace-page.component';
import { NachaConfigProfilesPageComponent } from './pages/nacha-config-profiles-page.component';
import { NachaConfigVariantsFieldsPageComponent } from './pages/nacha-config-variants-fields-page.component';

export const NACHA_CONFIG_ADMIN_ROUTES: Routes = [
  {
    path: 'perfiles',
    component: NachaConfigProfilesPageComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      roles: ['Admin', 'ACH.Operator'],
      permissions: ['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch'],
      title: 'Configuración NACHA-M',
      breadcrumb: 'Configuración NACHA-M'
    }
  },
  {
    path: 'records',
    component: NachaConfigRecordsPageComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      roles: ['Admin', 'ACH.Operator'],
      permissions: ['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch'],
      title: 'Registros oficiales',
      breadcrumb: 'Registros oficiales'
    }
  },
  {
    path: 'variants-fields',
    component: NachaConfigVariantsFieldsPageComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      roles: ['Admin', 'ACH.Operator'],
      permissions: ['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch'],
      title: 'Variantes y campos',
      breadcrumb: 'Variantes y campos'
    }
  },
  {
    path: 'perfiles/:id',
    component: NachaConfigProfileWorkspacePageComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      roles: ['Admin', 'ACH.Operator'],
      permissions: ['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch'],
      title: 'Perfil NACHA-M',
      breadcrumb: 'Detalle de perfil'
    }
  },
  { path: '', pathMatch: 'full', redirectTo: 'perfiles' }
];

@NgModule({
  imports: [RouterModule.forChild(NACHA_CONFIG_ADMIN_ROUTES)],
  exports: [RouterModule]
})
export class NachaConfigAdminRoutingModule {}
