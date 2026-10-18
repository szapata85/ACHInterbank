import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { roleGuard } from '../../core/guards/role.guard';
import { NachaConfigProfileWorkspacePageComponent } from './pages/nacha-config-profile-workspace-page.component';
import { NachaConfigProfilesPageComponent } from './pages/nacha-config-profiles-page.component';
import { NachaConfigLegacyWorkspaceRedirectComponent } from './workspace/nacha-config-legacy-workspace-redirect.component';

const access = {
  roles: ['Admin', 'ACH.Operator'],
  permissions: ['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch']
};

export const NACHA_CONFIG_ADMIN_ROUTES: Routes = [
  {
    path: 'perfiles',
    component: NachaConfigProfilesPageComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      ...access,
      title: 'Perfiles oficiales',
      breadcrumb: 'Perfiles oficiales'
    }
  },
  {
    path: 'records',
    component: NachaConfigLegacyWorkspaceRedirectComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      ...access,
      title: 'Registros oficiales',
      breadcrumb: 'Registros oficiales'
    }
  },
  {
    path: 'variants-fields',
    component: NachaConfigLegacyWorkspaceRedirectComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      ...access,
      title: 'Campos y variantes',
      breadcrumb: 'Campos y variantes'
    }
  },
  {
    path: 'perfiles/:id',
    component: NachaConfigProfileWorkspacePageComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      ...access,
      title: 'Parametrización NACHA-M',
      breadcrumb: 'Área de trabajo'
    }
  },
  { path: '', pathMatch: 'full', redirectTo: 'perfiles' }
];

@NgModule({
  imports: [RouterModule.forChild(NACHA_CONFIG_ADMIN_ROUTES)],
  exports: [RouterModule]
})
export class NachaConfigAdminRoutingModule {}
