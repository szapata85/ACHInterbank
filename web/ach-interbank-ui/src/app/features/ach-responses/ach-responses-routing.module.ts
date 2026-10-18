import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AchResponseAttemptsPageComponent } from './pages/ach-response-attempts-page.component';
import { AchResponseDashboardPageComponent } from './pages/ach-response-dashboard-page.component';
import { AchResponseDetailPageComponent } from './pages/ach-response-detail-page.component';
import { AchResponseListPageComponent } from './pages/ach-response-list-page.component';
import { AchResponseManualReviewPageComponent } from './pages/ach-response-manual-review-page.component';
import { AchResponseStatusMappingsPageComponent } from './pages/ach-response-status-mappings-page.component';
import { AchResponseAuditPageComponent } from './pages/ach-response-audit-page.component';
import { permissionGuard } from '../../core/guards/permission.guard';

export const ACH_RESPONSES_ROUTES: Routes = [
  {
    path: '',
    component: AchResponseListPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], title: 'Bandeja de respuestas', breadcrumb: 'Bandeja de respuestas' }
  },
  {
    path: 'manual-review',
    component: AchResponseManualReviewPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], title: 'Revisión manual', breadcrumb: 'Revisión manual' }
  },
  {
    path: 'status-mappings',
    component: AchResponseStatusMappingsPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], title: 'Homologación de estados', breadcrumb: 'Homologación de estados' }
  },
  {
    path: 'dashboard',
    component: AchResponseDashboardPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], title: 'Resumen de respuestas', breadcrumb: 'Resumen de respuestas' }
  },
  {
    path: 'audit',
    component: AchResponseAuditPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], title: 'Historial de respuestas', breadcrumb: 'Historial de respuestas' }
  },
  {
    path: ':id/notification-attempts',
    component: AchResponseAttemptsPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], title: 'Intentos de notificación ACH', breadcrumb: 'Intentos' }
  },
  {
    path: ':id',
    component: AchResponseDetailPageComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadAch'], title: 'Detalle respuesta ACH', breadcrumb: 'Detalle' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(ACH_RESPONSES_ROUTES)],
  exports: [RouterModule]
})
export class AchResponsesRoutingModule {}
