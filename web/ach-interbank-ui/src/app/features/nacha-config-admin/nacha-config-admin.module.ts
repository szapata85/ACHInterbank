import { NgModule } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { SharedModule } from '../../shared/shared.module';
import { NachaConfigAdminRoutingModule } from './nacha-config-admin-routing.module';
import { NachaConfigProfileWorkspacePageComponent } from './pages/nacha-config-profile-workspace-page.component';
import { NachaConfigProfilesPageComponent } from './pages/nacha-config-profiles-page.component';
import { NachaConfigContextBarComponent } from './workspace/nacha-config-context-bar.component';
import { NachaConfigFieldEditorComponent } from './workspace/nacha-config-field-editor.component';
import { NachaConfigFieldListComponent } from './workspace/nacha-config-field-list.component';
import { NachaConfigLifecycleActionsComponent } from './workspace/nacha-config-lifecycle-actions.component';
import { NachaConfigProfileMetadataComponent } from './workspace/nacha-config-profile-metadata.component';
import { NachaConfigPositionRulerComponent } from './workspace/nacha-config-position-ruler.component';
import { NachaConfigRecordListComponent } from './workspace/nacha-config-record-list.component';
import { NachaConfigTestPanelComponent } from './workspace/nacha-config-test-panel.component';
import { NachaConfigChangesPanelComponent } from './workspace/nacha-config-changes-panel.component';
import { NachaConfigHistoryPanelComponent } from './workspace/nacha-config-history-panel.component';

@NgModule({
  declarations: [NachaConfigProfilesPageComponent, NachaConfigProfileWorkspacePageComponent],
  imports: [
    SharedModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatSelectModule,
    MatTabsModule,
    NachaConfigContextBarComponent,
    NachaConfigRecordListComponent,
    NachaConfigFieldListComponent,
    NachaConfigFieldEditorComponent,
    NachaConfigPositionRulerComponent,
    NachaConfigProfileMetadataComponent,
    NachaConfigLifecycleActionsComponent,
    NachaConfigTestPanelComponent,
    NachaConfigChangesPanelComponent,
    NachaConfigHistoryPanelComponent,
    NachaConfigAdminRoutingModule
  ]
})
export class NachaConfigAdminModule {}
