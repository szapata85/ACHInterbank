import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { NachaConfigAdminRoutingModule } from './nacha-config-admin-routing.module';
import { NachaConfigProfileWorkspacePageComponent } from './pages/nacha-config-profile-workspace-page.component';
import { NachaConfigProfilesPageComponent } from './pages/nacha-config-profiles-page.component';

@NgModule({
  declarations: [NachaConfigProfilesPageComponent, NachaConfigProfileWorkspacePageComponent],
  imports: [SharedModule, NachaConfigAdminRoutingModule]
})
export class NachaConfigAdminModule {}
