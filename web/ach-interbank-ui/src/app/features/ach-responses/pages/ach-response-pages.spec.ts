import { TestBed } from '@angular/core/testing';
import { AchResponseAttemptsPageComponent } from './ach-response-attempts-page.component';
import { AchResponseDashboardPageComponent } from './ach-response-dashboard-page.component';
import { AchResponseManualReviewPageComponent } from './ach-response-manual-review-page.component';
import { AchResponseStatusMappingsPageComponent } from './ach-response-status-mappings-page.component';

describe('AchResponses Pages', () => {
  it('AchResponseAttemptsPageComponent_ShouldCreate', () => {
    const fixture = TestBed.configureTestingModule({ imports: [AchResponseAttemptsPageComponent] }).createComponent(AchResponseAttemptsPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('AchResponseManualReviewPageComponent_ShouldCreate', () => {
    const fixture = TestBed.configureTestingModule({ imports: [AchResponseManualReviewPageComponent] }).createComponent(AchResponseManualReviewPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('AchResponseStatusMappingsPageComponent_ShouldCreate', () => {
    const fixture = TestBed.configureTestingModule({ imports: [AchResponseStatusMappingsPageComponent] }).createComponent(AchResponseStatusMappingsPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('AchResponseDashboardPageComponent_ShouldCreate', () => {
    const fixture = TestBed.configureTestingModule({ imports: [AchResponseDashboardPageComponent] }).createComponent(AchResponseDashboardPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
