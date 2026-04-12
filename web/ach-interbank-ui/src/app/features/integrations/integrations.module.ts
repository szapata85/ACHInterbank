import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { IntegrationsRoutingModule } from './integrations-routing.module';
import { IntegrationWorkspaceComponent } from './pages/integration-workspace.component';
import { MappingSetsPageComponent } from './pages/mapping-sets-page.component';
import { MappingEditorPageComponent } from './pages/mapping-editor-page.component';
import { MappingComparePageComponent } from './pages/mapping-compare-page.component';

@NgModule({
  declarations: [IntegrationWorkspaceComponent, MappingSetsPageComponent, MappingEditorPageComponent, MappingComparePageComponent],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, SharedModule, IntegrationsRoutingModule]
})
export class IntegrationsModule {}
