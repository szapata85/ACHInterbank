import { TestBed } from '@angular/core/testing';
import { AchResponseDashboardPageComponent } from './ach-response-dashboard-page.component';

describe('AchResponses Pages', () => {
  it('AchResponseDashboardPageComponent_ShouldCreate', () => {
    const fixture = TestBed.configureTestingModule({ imports: [AchResponseDashboardPageComponent] }).createComponent(AchResponseDashboardPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
