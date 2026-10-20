import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { AchClaimsDetailComponent } from './ach-claims-detail.component';
import { AchClaimsListComponent } from './ach-claims-list.component';

export const ACH_CLAIMS_ROUTES: Routes = [
  {
    path: '',
    component: AchClaimsListComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['Claims.Read'],
      title: 'Reclamaciones',
      breadcrumb: 'Reclamaciones'
    }
  },
  {
    path: ':id',
    component: AchClaimsDetailComponent,
    canActivate: [permissionGuard],
    data: {
      permissions: ['Claims.Read'],
      title: 'Detalle de reclamación ACH',
      breadcrumb: 'Detalle'
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(ACH_CLAIMS_ROUTES)],
  exports: [RouterModule]
})
export class AchClaimsRoutingModule {}
