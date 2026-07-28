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
        data: { breadcrumb: 'Configuración de servicios SOAP', title: 'Configuración de servicios SOAP' }
      },
      {
        path: 'mappings',
        component: MappingSetsPageComponent,
        data: { breadcrumb: 'Matriz de campos SOAP', title: 'Matriz de campos SOAP' }
      },
      {
        path: 'mappings/:methodCode/:mappingSetId',
        component: MappingEditorPageComponent,
        data: { breadcrumb: 'Editor avanzado', title: 'Editor avanzado de relación de campos' }
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class IntegrationsRoutingModule {}
