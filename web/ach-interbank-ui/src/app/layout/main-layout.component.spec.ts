import { Component } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
import { BrandingService } from '../core/services/branding.service';
import { NavigationService } from '../core/services/navigation.service';
import { MainLayoutComponent } from './main-layout.component';

@Component({ standalone: true, template: '' })
class EmptyRouteComponent {}

describe('MainLayoutComponent navigation icons', () => {
  let fixture: ComponentFixture<MainLayoutComponent>;

  beforeEach(async () => {
    const navigationService = jasmine.createSpyObj<NavigationService>('NavigationService', ['getMenu']);
    navigationService.getMenu.and.returnValue(of([
      { id: 1, label: 'Panel principal', route: '/dashboard', icon: 'dashboard', exact: true },
      {
        id: 2,
        label: 'Operación',
        route: '/transactions',
        icon: 'account_balance',
        children: [{ id: 21, label: 'Ciclos', route: '/dashboard', icon: 'schedule', exact: true }]
      }
    ]));

    const authService = {
      user$: of({ fullName: 'Usuario UI', roles: ['Admin'] }),
      logout: jasmine.createSpy('logout')
    };
    const brandingService = {
      branding$: of({}),
      getBrandingSnapshot: () => ({})
    };

    await TestBed.configureTestingModule({
      imports: [MainLayoutComponent],
      providers: [
        provideRouter([{ path: 'dashboard', component: EmptyRouteComponent }]),
        { provide: NavigationService, useValue: navigationService },
        { provide: AuthService, useValue: authService },
        { provide: BrandingService, useValue: brandingService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MainLayoutComponent);
  });

  it('renders configured root and child icons while preserving expanded labels and active state', fakeAsync(() => {
    const router = TestBed.inject(Router);
    fixture.detectChanges();
    void router.navigateByUrl('/dashboard');
    tick();
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('a[data-menu-item-id="1"] app-ui-icon')?.getAttribute('data-icon-key')).toBe('dashboard');
    expect(host.querySelector('button[data-menu-item-id="2"] app-ui-icon')?.getAttribute('data-icon-key')).toBe('account_balance');
    expect(host.querySelector('a[data-menu-item-id="21"] app-ui-icon')?.getAttribute('data-icon-key')).toBe('schedule');
    expect(host.querySelector('a[data-menu-item-id="1"] .label')?.textContent?.trim()).toBe('Panel principal');
    expect(host.querySelector('a[data-menu-item-id="21"] .label')?.textContent?.trim()).toBe('Ciclos');
    expect(host.querySelectorAll('a.active').length).toBeGreaterThan(0);
  }));

  it('keeps root icons in the DOM when the desktop sidebar is collapsed', () => {
    fixture.detectChanges();
    const component = fixture.componentInstance;
    spyOnProperty(window, 'innerWidth', 'get').and.returnValue(1366);

    component.toggleMenu();
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.layout')?.classList.contains('sidebar-collapsed')).toBeTrue();
    expect(host.querySelector('a[data-menu-item-id="1"] app-ui-icon')?.getAttribute('data-icon-key')).toBe('dashboard');
    expect(component.menuToggleLabel).toBe('Expandir menú principal');
  });

  it('uses a native button row as the only submenu control', () => {
    fixture.detectChanges();
    const component = fixture.componentInstance;
    const host = fixture.nativeElement as HTMLElement;
    const parent = host.querySelector('button[data-menu-item-id="2"]') as HTMLButtonElement;

    expect(host.querySelectorAll('.menu-header > button').length).toBe(1);
    expect(parent.getAttribute('href')).toBeNull();
    expect(parent.type).toBe('button');
    expect(parent.getAttribute('aria-controls')).toBe('submenu-2');
    expect(parent.querySelector('.chevron')?.getAttribute('aria-hidden')).toBe('true');
    expect(parent.tabIndex).toBe(0);

    parent.click();
    fixture.detectChanges();
    expect(parent.getAttribute('aria-expanded')).toBe('true');

    parent.click();
    fixture.detectChanges();
    expect(parent.getAttribute('aria-expanded')).toBe('false');
  });

  it('keeps navigable items as links and closes the mobile menu only for navigation', () => {
    fixture.detectChanges();
    const component = fixture.componentInstance;
    const host = fixture.nativeElement as HTMLElement;
    const parent = host.querySelector('button[data-menu-item-id="2"]') as HTMLButtonElement;
    const navigable = host.querySelector('a[data-menu-item-id="1"]') as HTMLAnchorElement;
    spyOnProperty(window, 'innerWidth', 'get').and.returnValue(390);
    component.isMenuOpen = true;
    const closeMenu = spyOn(component, 'closeMenu').and.callThrough();

    parent.click();
    fixture.detectChanges();
    expect(component.isMenuOpen).toBeTrue();
    expect(closeMenu).not.toHaveBeenCalled();

    navigable.click();
    expect(closeMenu).toHaveBeenCalled();
    expect(navigable.getAttribute('href')).toBe('/dashboard');
  });

  it('keeps keyboard activation native through button semantics', () => {
    fixture.detectChanges();
    const parent = fixture.nativeElement.querySelector('button[data-menu-item-id="2"]') as HTMLButtonElement;

    expect(parent.type).toBe('button');
    expect(parent.tabIndex).toBe(0);
    expect(parent.querySelectorAll('button, a, [tabindex]:not([tabindex="-1"])').length).toBe(0);
  });
});
