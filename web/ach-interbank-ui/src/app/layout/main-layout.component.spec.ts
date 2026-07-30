import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { Component } from '@angular/core';
import { ComponentFixture, fakeAsync, TestBed, tick } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
import { BrandingService } from '../core/services/branding.service';
import { MenuItem } from '../core/models/menu.model';
import { NavigationService } from '../core/services/navigation.service';
import { MainLayoutComponent } from './main-layout.component';

@Component({ standalone: true, template: '<p>Contenido de prueba</p>' })
class EmptyRouteComponent {}

describe('MainLayoutComponent', () => {
  let fixture: ComponentFixture<MainLayoutComponent>;
  let component: MainLayoutComponent;
  let router: Router;
  let authService: jasmine.SpyObj<AuthService>;
  let navigationService: jasmine.SpyObj<NavigationService>;
  let menuSource: BehaviorSubject<MenuItem[]>;
  let breakpointSource: BehaviorSubject<BreakpointState>;

  const menu: MenuItem[] = [
    {
      id: 1,
      label: 'Panel principal',
      route: '/dashboard',
      icon: 'dashboard',
      exact: true
    },
    {
      id: 2,
      label: 'Operación',
      route: '/transactions',
      icon: 'account_balance',
      children: [
        {
          id: 21,
          label: 'Ciclos',
          route: '/transactions/cycles',
          icon: 'schedule',
          children: [
            {
              id: 211,
              label: 'Detalle de ciclo',
              route: '/transactions/cycles/detail',
              icon: 'description',
              exact: true
            }
          ]
        },
        {
          id: 22,
          label: 'Auditoría',
          route: '/audit-logs',
          icon: 'fact_check',
          exact: true
        }
      ]
    }
  ];

  beforeEach(async () => {
    menuSource = new BehaviorSubject<MenuItem[]>(menu);
    breakpointSource = new BehaviorSubject<BreakpointState>({
      matches: false,
      breakpoints: { '(max-width: 959.98px)': false }
    });

    navigationService = jasmine.createSpyObj<NavigationService>('NavigationService', ['getMenu']);
    navigationService.getMenu.and.returnValue(menuSource.asObservable());

    authService = jasmine.createSpyObj<AuthService>(
      'AuthService',
      ['logout'],
      {
        user$: of({
          token: 'test-token',
          username: 'operador',
          fullName: 'Usuario UI',
          roles: ['Admin'],
          permissions: []
        })
      }
    );

    const brandingService = {
      branding$: of({}),
      getBrandingSnapshot: () => ({})
    };

    const breakpointObserver = jasmine.createSpyObj<BreakpointObserver>(
      'BreakpointObserver',
      ['observe']
    );
    breakpointObserver.observe.and.returnValue(breakpointSource.asObservable());

    await TestBed.configureTestingModule({
      imports: [MainLayoutComponent, NoopAnimationsModule],
      providers: [
        provideRouter([
          { path: 'dashboard', component: EmptyRouteComponent },
          { path: 'transactions/cycles/detail', component: EmptyRouteComponent },
          { path: 'audit-logs', component: EmptyRouteComponent }
        ]),
        { provide: NavigationService, useValue: navigationService },
        { provide: AuthService, useValue: authService },
        { provide: BrandingService, useValue: brandingService },
        { provide: BreakpointObserver, useValue: breakpointObserver }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MainLayoutComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
  });

  it('renders the authorized dynamic source without adding hardcoded options', () => {
    menuSource.next([menu[0]]);
    fixture.detectChanges();

    const links = fixture.nativeElement.querySelectorAll(
      '[data-menu-item-id]'
    ) as NodeListOf<HTMLElement>;

    expect(navigationService.getMenu).toHaveBeenCalledTimes(1);
    expect(links.length).toBe(1);
    expect(links[0].textContent).toContain('Panel principal');
    expect(fixture.nativeElement.textContent).not.toContain('Operación');
  });

  it('renders every hierarchy level and exposes the active destination accessibly', fakeAsync(() => {
    fixture.detectChanges();
    void router.navigateByUrl('/transactions/cycles/detail?view=compact');
    tick();
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const operation = host.querySelector(
      'button[data-menu-item-id="2"]'
    ) as HTMLButtonElement;
    const cycles = host.querySelector(
      'button[data-menu-item-id="21"]'
    ) as HTMLButtonElement;
    const detail = host.querySelector(
      'a[data-menu-item-id="211"]'
    ) as HTMLAnchorElement;

    expect(operation).not.toBeNull();
    expect(cycles).not.toBeNull();
    expect(detail).not.toBeNull();
    expect(operation.getAttribute('aria-expanded')).toBe('true');
    expect(cycles.getAttribute('aria-expanded')).toBe('true');
    expect(detail.getAttribute('aria-current')).toBe('page');
    expect(detail.classList.contains('active')).toBeTrue();
    expect(component.activeItemIds.has(2)).toBeTrue();
    expect(component.activeItemIds.has(21)).toBeTrue();
    expect(component.activeItemIds.has(211)).toBeTrue();
  }));

  it('collapses and expands the desktop sidenav while keeping root icons available', () => {
    fixture.detectChanges();
    const toggle = menuToggle();

    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    toggle.click();
    fixture.detectChanges();

    const sidenav = fixture.nativeElement.querySelector('.primary-sidenav') as HTMLElement;
    expect(component.isSidebarCollapsed).toBeTrue();
    expect(sidenav.classList.contains('collapsed')).toBeTrue();
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(toggle.getAttribute('aria-controls')).toBe('primary-navigation');
    expect(component.menuToggleLabel).toBe('Expandir menú principal');
    expect(
      sidenav
        .querySelector('a[data-menu-item-id="1"] app-ui-icon')
        ?.getAttribute('data-icon-key')
    ).toBe('dashboard');

    toggle.click();
    fixture.detectChanges();

    expect(component.isSidebarCollapsed).toBeFalse();
  });

  it('keeps a mobile submenu open for touch and closes the drawer after navigation', fakeAsync(() => {
    setMobile(true);
    fixture.detectChanges();

    menuToggle().click();
    fixture.detectChanges();
    tick();

    expect(component.isMenuOpen).toBeTrue();

    const operation = fixture.nativeElement.querySelector(
      'button[data-menu-item-id="2"]'
    ) as HTMLButtonElement;
    operation.click();
    fixture.detectChanges();

    expect(component.isMenuOpen).toBeTrue();
    expect(operation.getAttribute('aria-expanded')).toBe('true');

    const audit = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="22"]'
    ) as HTMLAnchorElement;
    audit.click();
    tick();
    fixture.detectChanges();

    expect(router.url).toBe('/audit-logs');
    expect(component.isMenuOpen).toBeFalse();
  }));

  it('lets Material close the mobile sidenav with Escape and with its backdrop', fakeAsync(() => {
    setMobile(true);
    fixture.detectChanges();

    menuToggle().click();
    fixture.detectChanges();
    tick();

    const sidenav = fixture.nativeElement.querySelector('.primary-sidenav') as HTMLElement;
    const escapeEvent = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true });
    Object.defineProperty(escapeEvent, 'keyCode', { get: () => 27 });
    sidenav.dispatchEvent(escapeEvent);
    tick();
    fixture.detectChanges();

    expect(component.isMenuOpen).toBeFalse();

    menuToggle().click();
    fixture.detectChanges();
    tick();

    const backdrop = fixture.nativeElement.querySelector(
      '.mat-drawer-backdrop'
    ) as HTMLElement;
    expect(backdrop).not.toBeNull();
    backdrop.click();
    tick();
    fixture.detectChanges();

    expect(component.isMenuOpen).toBeFalse();
  }));

  it('switches deterministically between side and overlay behavior at the shared breakpoint', () => {
    fixture.detectChanges();
    const sidenav = fixture.nativeElement.querySelector('mat-sidenav') as HTMLElement;

    expect(component.isMobile).toBeFalse();
    expect(sidenav.classList.contains('mat-drawer-side')).toBeTrue();

    setMobile(true);
    fixture.detectChanges();

    expect(component.isMobile).toBeTrue();
    expect(component.isSidebarCollapsed).toBeFalse();
    expect(component.isMenuOpen).toBeFalse();
    expect(sidenav.classList.contains('mat-drawer-over')).toBeTrue();
  });

  it('delegates logout to the existing authentication service', () => {
    fixture.detectChanges();

    const logout = fixture.nativeElement.querySelector(
      'button.logout-button'
    ) as HTMLButtonElement;
    logout.click();

    expect(authService.logout).toHaveBeenCalledTimes(1);
  });

  it('shows an accessible status instead of leaving an unhandled menu error', () => {
    menuSource.error(new Error('backend details'));
    fixture.detectChanges();

    const status = fixture.nativeElement.querySelector('[role="status"]') as HTMLElement;
    expect(component.menuItems).toEqual([]);
    expect(status.textContent).toContain('No fue posible cargar el menú principal');
    expect(status.textContent).not.toContain('backend details');
  });

  function setMobile(matches: boolean): void {
    breakpointSource.next({
      matches,
      breakpoints: { '(max-width: 959.98px)': matches }
    });
  }

  function menuToggle(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button.menu-toggle') as HTMLButtonElement;
  }
});
