import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { IntegrationWorkspaceComponent } from './pages/integration-workspace.component';
import { MappingSetsPageComponent } from './pages/mapping-sets-page.component';
import { MappingEditorPageComponent } from './pages/mapping-editor-page.component';
import { MappingComparePageComponent } from './pages/mapping-compare-page.component';
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
        data: { breadcrumb: 'Configuracion de servicios SOAP', title: 'Configuracion de servicios SOAP' }
      },
      {
        path: 'mappings',
        component: MappingSetsPageComponent,
        data: { breadcrumb: 'Matriz de campos SOAP', title: 'Matriz de campos SOAP' }
      },
      {
        path: 'mappings/compare/:methodCode',
        component: MappingComparePageComponent,
        data: { breadcrumb: 'Comparacion tecnica', title: 'Comparacion tecnica de relaciones' }
      },
      {
        path: 'mappings/:methodCode/:mappingSetId',
        component: MappingEditorPageComponent,
        data: { breadcrumb: 'Editor avanzado', title: 'Editor avanzado de relacion de campos' }
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class IntegrationsRoutingModule {}
