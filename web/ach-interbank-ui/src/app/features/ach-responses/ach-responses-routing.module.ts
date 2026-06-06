import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AchResponseAttemptsPageComponent } from './pages/ach-response-attempts-page.component';
import { AchResponseDashboardPageComponent } from './pages/ach-response-dashboard-page.component';
import { AchResponseDetailPageComponent } from './pages/ach-response-detail-page.component';
import { AchResponseListPageComponent } from './pages/ach-response-list-page.component';
import { AchResponseManualReviewPageComponent } from './pages/ach-response-manual-review-page.component';
import { AchResponseStatusMappingsPageComponent } from './pages/ach-response-status-mappings-page.component';

export const ACH_RESPONSES_ROUTES: Routes = [
  {
    path: '',
    component: AchResponseListPageComponent,
    data: { title: 'Respuestas ACH', breadcrumb: 'Bandeja' }
  },
  {
    path: 'manual-review',
    component: AchResponseManualReviewPageComponent,
    data: { title: 'Revisión manual ACH', breadcrumb: 'Revisión manual' }
  },
  {
    path: 'status-mappings',
    component: AchResponseStatusMappingsPageComponent,
    data: { title: 'Homologaciones ACH', breadcrumb: 'Homologaciones' }
  },
  {
    path: 'dashboard',
    component: AchResponseDashboardPageComponent,
    data: { title: 'Panel de respuestas ACH', breadcrumb: 'Panel' }
  },
  {
    path: ':id/notification-attempts',
    component: AchResponseAttemptsPageComponent,
    data: { title: 'Intentos de notificación ACH', breadcrumb: 'Intentos' }
  },
  {
    path: ':id',
    component: AchResponseDetailPageComponent,
    data: { title: 'Detalle respuesta ACH', breadcrumb: 'Detalle' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(ACH_RESPONSES_ROUTES)],
  exports: [RouterModule]
})
export class AchResponsesRoutingModule {}
