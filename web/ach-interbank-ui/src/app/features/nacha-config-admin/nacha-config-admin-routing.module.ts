import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { roleGuard } from '../../core/guards/role.guard';
import { NachaConfigProfileWorkspacePageComponent } from './pages/nacha-config-profile-workspace-page.component';
import { NachaConfigProfilesPageComponent } from './pages/nacha-config-profiles-page.component';

const routes: Routes = [
  {
    path: 'perfiles',
    component: NachaConfigProfilesPageComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      roles: ['Admin', 'ACH.Operator'],
      permissions: ['CanReadAch', 'CanManageAch'],
      title: 'Perfiles NACHA',
      breadcrumb: 'Perfiles NACHA'
    }
  },
  {
    path: 'perfiles/:id',
    component: NachaConfigProfileWorkspacePageComponent,
    canActivate: [roleGuard, permissionGuard],
    data: {
      roles: ['Admin', 'ACH.Operator'],
      permissions: ['CanReadAch', 'CanManageAch'],
      title: 'Perfil NACHA',
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
