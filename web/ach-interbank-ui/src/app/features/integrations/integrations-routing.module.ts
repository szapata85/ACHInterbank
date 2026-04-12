import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { IntegrationWorkspaceComponent } from './pages/integration-workspace.component';
import { MappingSetsPageComponent } from './pages/mapping-sets-page.component';
import { MappingEditorPageComponent } from './pages/mapping-editor-page.component';
import { SoapIntegrationSettingsComponent } from '../admin/components/soap-integration-settings.component';

const routes: Routes = [
  {
    path: '',
    component: IntegrationWorkspaceComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'mappings' },
      {
        path: 'soap-settings',
        component: SoapIntegrationSettingsComponent,
        data: { breadcrumb: 'Configuración técnica SOAP', title: 'Configuración técnica SOAP' }
      },
      {
        path: 'mappings',
        component: MappingSetsPageComponent,
        data: { breadcrumb: 'Mapping funcional', title: 'Mapping funcional de integraciones' }
      },
      {
        path: 'mappings/:methodCode/:mappingSetId',
        component: MappingEditorPageComponent,
        data: { breadcrumb: 'Editor de mapping', title: 'Editor de MappingSet' }
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class IntegrationsRoutingModule {}
