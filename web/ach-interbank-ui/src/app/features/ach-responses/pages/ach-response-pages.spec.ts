import { TestBed } from '@angular/core/testing';
import { AchResponseDashboardPageComponent } from './ach-response-dashboard-page.component';
import { AchResponseStatusMappingsPageComponent } from './ach-response-status-mappings-page.component';

describe('AchResponses Pages', () => {
  it('AchResponseStatusMappingsPageComponent_ShouldCreate', () => {
    const fixture = TestBed.configureTestingModule({ imports: [AchResponseStatusMappingsPageComponent] }).createComponent(AchResponseStatusMappingsPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('AchResponseDashboardPageComponent_ShouldCreate', () => {
    const fixture = TestBed.configureTestingModule({ imports: [AchResponseDashboardPageComponent] }).createComponent(AchResponseDashboardPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
