import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Component } from '@angular/core';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter, Router } from '@angular/router';
import { ClearingHouseContextNavigationComponent } from './clearing-house-context-navigation.component';

@Component({ standalone: true, template: '' })
class EmptyRouteComponent {}

describe('ClearingHouseContextNavigationComponent', () => {
  let fixture: ComponentFixture<ClearingHouseContextNavigationComponent>;
  let component: ClearingHouseContextNavigationComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ClearingHouseContextNavigationComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([
          { path: 'clearing-houses', component: EmptyRouteComponent },
          { path: 'clearing-houses/:id/transaction-policies', component: EmptyRouteComponent },
          { path: 'clearing-houses/:id/cycles', component: EmptyRouteComponent },
          { path: 'clearing-houses/:id/special-dates', component: EmptyRouteComponent }
        ])
      ]
    }).compileComponents();
    fixture = TestBed.createComponent(ClearingHouseContextNavigationComponent);
    component = fixture.componentInstance;
    component.clearingHouse = { id: 7, code: 'ACHCOL', name: 'ACH Colombia', isActive: true } as any;
    component.canReadPolicies = true;
    component.canReadCycles = true;
    component.canReadSpecialDates = true;
    fixture.detectChanges();
  });

  it('renders the three contextual routes and a return link', () => {
    const links = Array.from(fixture.nativeElement.querySelectorAll('a')).map((link: any) => link.getAttribute('href'));
    expect(links).toContain('/clearing-houses');
    expect(links).toContain('/clearing-houses/7/transaction-policies');
    expect(links).toContain('/clearing-houses/7/cycles');
    expect(links).toContain('/clearing-houses/7/special-dates');
    expect(fixture.nativeElement.textContent).toContain('ACH Colombia');
    expect(fixture.nativeElement.textContent).toContain('ACHCOL');
  });

  it('filters links with real permissions', () => {
    fixture.componentRef.setInput('canReadCycles', false);
    fixture.componentRef.setInput('canReadSpecialDates', false);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('a[href="/clearing-houses/7/cycles"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('a[href="/clearing-houses/7/special-dates"]')).toBeNull();
  });

  it('marks the active link and keeps semantic keyboard-focusable anchors', fakeAsync(() => {
    const router = TestBed.inject(Router);
    void router.navigateByUrl('/clearing-houses/7/cycles');
    tick();
    fixture.detectChanges();
    const active = fixture.nativeElement.querySelector('a[href="/clearing-houses/7/cycles"]') as HTMLAnchorElement;
    expect(active.getAttribute('aria-current')).toBe('page');
    expect(active.tabIndex).toBeGreaterThanOrEqual(0);
  }));
});
