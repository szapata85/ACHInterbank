import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { roleGuard } from '../../core/guards/role.guard';
import { NachaConfigRecordsPageComponent } from './pages/nacha-config-records-page.component';
import { NachaConfigProfileWorkspacePageComponent } from './pages/nacha-config-profile-workspace-page.component';
import { NachaConfigProfilesPageComponent } from './pages/nacha-config-profiles-page.component';
import { NachaConfigVariantsFieldsPageComponent } from './pages/nacha-config-variants-fields-page.component';

const routes: Routes = [
  {
    path: 'perfiles',
    component: NachaConfigProfilesPageComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      roles: ['Admin', 'ACH.Operator'],
      permissions: ['CanReadAch'],
      title: 'Config Profiles NACHA',
      breadcrumb: 'Config Profiles'
    }
  },
  {
    path: 'records',
    component: NachaConfigRecordsPageComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      roles: ['Admin', 'ACH.Operator'],
      permissions: ['CanReadAch'],
      title: 'NACHA Config - Records',
      breadcrumb: 'Records oficiales'
    }
  },
  {
    path: 'variants-fields',
    component: NachaConfigVariantsFieldsPageComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      roles: ['Admin', 'ACH.Operator'],
      permissions: ['CanReadAch'],
      title: 'NACHA Config - Variants y Fields',
      breadcrumb: 'Variants y Fields'
    }
  },
  {
    path: 'perfiles/:id',
    component: NachaConfigProfileWorkspacePageComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      roles: ['Admin', 'ACH.Operator'],
      permissions: ['CanReadAch'],
      title: 'Perfil NACHA read-only',
      breadcrumb: 'Detalle de perfil'
    }
  },
  { path: '', pathMatch: 'full', redirectTo: 'perfiles' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NachaConfigAdminRoutingModule {}
