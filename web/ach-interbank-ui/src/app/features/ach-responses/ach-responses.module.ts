import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { AchResponsesRoutingModule } from './ach-responses-routing.module';
import { AchResponseAttemptsPageComponent } from './pages/ach-response-attempts-page.component';
import { AchResponseDashboardPageComponent } from './pages/ach-response-dashboard-page.component';
import { AchResponseDetailPageComponent } from './pages/ach-response-detail-page.component';
import { AchResponseListPageComponent } from './pages/ach-response-list-page.component';
import { AchResponseManualReviewPageComponent } from './pages/ach-response-manual-review-page.component';
import { AchResponseStatusMappingsPageComponent } from './pages/ach-response-status-mappings-page.component';
import { AchResponseAuditPageComponent } from './pages/ach-response-audit-page.component';

@NgModule({
  imports: [
    CommonModule,
    SharedModule,
    AchResponsesRoutingModule,
    AchResponseListPageComponent,
    AchResponseDetailPageComponent,
    AchResponseAttemptsPageComponent,
    AchResponseManualReviewPageComponent,
    AchResponseStatusMappingsPageComponent,
    AchResponseDashboardPageComponent,
    AchResponseAuditPageComponent
  ]
})
export class AchResponsesModule {}
