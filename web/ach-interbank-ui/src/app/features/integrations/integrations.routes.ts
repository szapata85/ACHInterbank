import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';

export const INTEGRATIONS_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./pages/integration-workspace.component').then((m) => m.IntegrationWorkspaceComponent)
  },
  {
    path: 'contrato',
    canActivate: [permissionGuard],
    data: {
      permissions: ['Integrations.Read', 'CanReadAch'],
      breadcrumb: 'Contrato del servicio',
      title: 'Contrato del servicio'
    },
    loadComponent: () =>
      import('./pages/contract-versions-page.component').then((m) => m.ContractVersionsPageComponent)
  },
  {
    path: 'relacion-informacion',
    canActivate: [permissionGuard],
    data: { permissions: ['Integrations.Read', 'CanReadAch'], breadcrumb: 'Relación de información', title: 'Relación de información' },
    loadComponent: () =>
      import('./pages/mapping-sets-page.component').then((m) => m.MappingSetsPageComponent)
  },
  {
    path: 'relacion-informacion/:methodCode/:mappingSetId',
    canActivate: [permissionGuard],
    data: { permissions: ['Integrations.Read', 'CanReadAch'], breadcrumb: 'Editor de relación de información', title: 'Editor de relación de información' },
    loadComponent: () =>
      import('./pages/mapping-editor-page.component').then((m) => m.MappingEditorPageComponent)
  },
  {
    path: 'conexion',
    canActivate: [permissionGuard],
    data: { permissions: ['Integrations.Read', 'CanReadAch'], breadcrumb: 'Conexión entidad participante', title: 'Conexión entidad participante' },
    loadComponent: () =>
      import('./pages/soap-integration-settings.component').then((m) => m.SoapIntegrationSettingsComponent)
  }
];
