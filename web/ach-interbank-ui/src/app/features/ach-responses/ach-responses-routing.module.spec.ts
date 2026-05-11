import { ACH_RESPONSES_ROUTES } from './ach-responses-routing.module';

describe('AchResponsesRoutingModule', () => {
  it('ShouldDefineExpectedRoutes', () => {
    const paths = ACH_RESPONSES_ROUTES.map((route) => route.path);

    expect(paths).toContain('');
    expect(paths).toContain('manual-review');
    expect(paths).toContain('status-mappings');
    expect(paths).toContain('dashboard');
    expect(paths).toContain(':id/notification-attempts');
    expect(paths).toContain(':id');
  });

  it('ShouldDefineRoutesInSafeOrder', () => {
    const paths = ACH_RESPONSES_ROUTES.map((route) => route.path);

    expect(paths).toEqual([
      '',
      'manual-review',
      'status-mappings',
      'dashboard',
      ':id/notification-attempts',
      ':id'
    ]);
  });
});
