import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { Component } from '@angular/core';
import { ComponentFixture, fakeAsync, flushMicrotasks, TestBed, tick } from '@angular/core/testing';
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
    },
    {
      id: 3,
      label: 'Catálogos',
      route: '/catalogs',
      icon: 'inventory',
      children: [
        {
          id: 31,
          label: 'Restricciones operacionales CFA',
          route: '/operational-restrictions',
          icon: 'rule_settings',
          exact: true
        }
      ]
    }
  ];

  const futureFunctionalParent: MenuItem = {
    id: 40,
    label: 'Parent futuro',
    route: '/future-parent',
    order: 10,
    children: [
      { id: 401, label: 'Child A', route: '/future-parent/a', order: 10, exact: true },
      { id: 402, label: 'Child B', route: '/future-parent/b', order: 20, exact: true }
    ]
  };

  const reportsMenu: MenuItem = {
    id: 70,
    label: 'Reportes',
    route: '/reports',
    order: 70,
    exact: false,
    children: [
      { id: 701, label: 'Transacciones enviadas', route: '/reports/sent', order: 10, exact: true },
      { id: 702, label: 'Transacciones recibidas', route: '/reports/received', order: 20, exact: true },
      { id: 703, label: 'Devoluciones', route: '/reports/returns', order: 30, exact: true },
      { id: 704, label: 'Rechazos', route: '/reports/rejections', order: 40, exact: true },
      { id: 705, label: 'Archivos NACHA-M', route: '/reports/files', order: 50, exact: true },
      { id: 706, label: 'Ciclos', route: '/reports/cycles', order: 60, exact: true },
      { id: 707, label: 'Conciliación', route: '/reports/reconciliation', order: 70, exact: true },
      { id: 708, label: 'Trazabilidad ACH', route: '/reports/traceability', order: 80, exact: true },
      { id: 709, label: 'Histórico', route: '/reports/history', order: 90, exact: true },
      { id: 710, label: 'Auditoría', route: '/reports/audit', order: 100, exact: true }
    ]
  };

  const connectionsMenu: MenuItem = {
    id: 50,
    label: 'Conexiones',
    route: '/integraciones',
    order: 80,
    exact: false,
    children: [
      { id: 501, label: 'Contrato del servicio', route: '/integraciones/contrato', order: 10, exact: true },
      { id: 502, label: 'Relación de información', route: '/integraciones/relacion-informacion', order: 20, exact: true },
      { id: 503, label: 'Conexión entidad participante', route: '/integraciones/conexion', order: 30, exact: true },
      { id: 504, label: 'Configuración del intercambio de archivos', route: '/administracion/mft-ach', order: 40, exact: true }
    ]
  };

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
      branding$: of({ privateLogo: 'data:image/png;base64,private-logo' }),
      getBrandingSnapshot: () => ({}),
      productName: 'ACH Interbank'
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
          {
            path: 'transactions',
            children: [
              { path: '', pathMatch: 'full', redirectTo: 'list' },
              { path: 'list', component: EmptyRouteComponent },
              { path: 'cycles/detail', component: EmptyRouteComponent }
            ]
          },
          {
            path: 'catalogs',
            children: [
              { path: '', component: EmptyRouteComponent }
            ]
          },
          { path: 'audit-logs', component: EmptyRouteComponent },
          { path: 'operational-restrictions', component: EmptyRouteComponent },
          {
            path: 'reports',
            children: [
              { path: '', component: EmptyRouteComponent },
              { path: 'sent', component: EmptyRouteComponent },
              { path: 'received', component: EmptyRouteComponent },
              { path: 'returns', component: EmptyRouteComponent },
              { path: 'rejections', component: EmptyRouteComponent },
              { path: 'files', component: EmptyRouteComponent },
              { path: 'cycles', component: EmptyRouteComponent },
              { path: 'reconciliation', component: EmptyRouteComponent },
              { path: 'traceability', component: EmptyRouteComponent },
              { path: 'history', component: EmptyRouteComponent },
              { path: 'audit', component: EmptyRouteComponent }
            ]
          },
          {
            path: 'integraciones',
            children: [
              { path: '', component: EmptyRouteComponent },
              { path: 'contrato', component: EmptyRouteComponent },
              { path: 'relacion-informacion', component: EmptyRouteComponent },
              { path: 'conexion', component: EmptyRouteComponent }
            ]
          },
          { path: 'administracion/mft-ach', component: EmptyRouteComponent },
          {
            path: 'future-parent',
            children: [
              { path: '', component: EmptyRouteComponent },
              { path: 'a', component: EmptyRouteComponent },
              { path: 'b', component: EmptyRouteComponent }
            ]
          }
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

  it('keeps a redirect-only parent structural even when its route is registered', fakeAsync(() => {
    fixture.detectChanges();
    flushMicrotasks();
    fixture.detectChanges();

    const parentButton = fixture.nativeElement.querySelector(
      'button[data-menu-item-id="2"]'
    ) as HTMLButtonElement;
    const parentLink = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="2"]'
    ) as HTMLAnchorElement | null;

    expect(parentButton).not.toBeNull();
    expect(parentButton.textContent).toContain('Operación');
    expect(parentLink).toBeNull();
    expect(parentButton.getAttribute('href')).toBeNull();
    expect(component.navigableParentIds.has(2)).toBeFalse();
  }));

  it('gives a functional future parent independent navigation and expansion controls without route hardcoding', fakeAsync(() => {
    menuSource.next([futureFunctionalParent]);
    fixture.detectChanges();
    flushMicrotasks();
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="40"]'
    ) as HTMLAnchorElement;
    const toggle = fixture.nativeElement.querySelector(
      'button[data-menu-toggle-id="40"]'
    ) as HTMLButtonElement;

    expect(component.navigableParentIds.has(40)).toBeTrue();
    expect(component.menuItems[0]).toEqual(futureFunctionalParent);
    expect(link).not.toBeNull();
    expect(link.textContent).toContain('Parent futuro');
    expect(toggle).not.toBeNull();
    expect(toggle.type).toBe('button');
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    expect(toggle.getAttribute('aria-controls')).toBe('submenu-40');
    expect(toggle.getAttribute('aria-label')).toBe('Expandir Parent futuro');
    toggle.focus();
    expect(document.activeElement).toBe(toggle);

    const urlBeforeToggle = router.url;
    toggle.click();
    fixture.detectChanges();

    expect(router.url).toBe(urlBeforeToggle);
    expect(component.expandedItems.has(40)).toBeTrue();
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    expect(fixture.nativeElement.querySelectorAll('#submenu-40 [data-menu-item-id]').length).toBe(2);

    link.click();
    tick();
    fixture.detectChanges();

    expect(router.url).toBe('/future-parent');
    expect(component.expandedItems.has(40)).toBeTrue();
    expect(link.getAttribute('aria-current')).toBe('page');
  }));

  it('keeps functional-parent navigation and expansion distinct in desktop compact mode', fakeAsync(() => {
    menuSource.next([futureFunctionalParent]);
    fixture.detectChanges();
    flushMicrotasks();
    fixture.detectChanges();

    component.toggleNavigation();
    fixture.detectChanges();
    expect(component.isDesktopCompact).toBeTrue();

    const link = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="40"]'
    ) as HTMLAnchorElement;
    const toggle = fixture.nativeElement.querySelector(
      'button[data-menu-toggle-id="40"]'
    ) as HTMLButtonElement;

    expect(link).not.toBeNull();
    expect(toggle).not.toBeNull();

    const urlBeforeToggle = router.url;
    toggle.click();
    fixture.detectChanges();

    expect(router.url).toBe(urlBeforeToggle);
    expect(component.isDesktopCompact).toBeFalse();
    expect(component.expandedItems.has(40)).toBeTrue();
  }));

  it('keeps functional-parent children reachable on mobile and closes only after parent navigation', fakeAsync(() => {
    menuSource.next([futureFunctionalParent]);
    fixture.detectChanges();
    flushMicrotasks();
    setMobile(true);
    fixture.detectChanges();

    menuToggle().click();
    fixture.detectChanges();
    tick();
    expect(component.isMobileDrawerOpen).toBeTrue();

    const link = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="40"]'
    ) as HTMLAnchorElement;
    const toggle = fixture.nativeElement.querySelector(
      'button[data-menu-toggle-id="40"]'
    ) as HTMLButtonElement;

    toggle.click();
    fixture.detectChanges();

    expect(component.isMobileDrawerOpen).toBeTrue();
    expect(component.expandedItems.has(40)).toBeTrue();
    expect(fixture.nativeElement.querySelectorAll('#submenu-40 [data-menu-item-id]').length).toBe(2);

    link.click();
    tick();
    fixture.detectChanges();

    expect(router.url).toBe('/future-parent');
    expect(component.isMobileDrawerOpen).toBeFalse();
  }));

  it('keeps a missing structural parent as an accessible expander without invalid navigation', fakeAsync(() => {
    menuSource.next([{
      id: 41,
      label: 'Grupo estructural',
      route: '/seguimiento-control',
      order: 10,
      children: [
        { id: 411, label: 'Destino', route: '/dashboard', order: 10, exact: true }
      ]
    }]);
    fixture.detectChanges();
    flushMicrotasks();
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector(
      'button[data-menu-item-id="41"]'
    ) as HTMLButtonElement;
    const link = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="41"]'
    ) as HTMLAnchorElement | null;
    const urlBeforeToggle = router.url;

    expect(button).not.toBeNull();
    expect(link).toBeNull();
    expect(button.getAttribute('aria-expanded')).toBe('false');
    expect(button.getAttribute('aria-controls')).toBe('submenu-41');

    button.click();
    fixture.detectChanges();

    expect(router.url).toBe(urlBeforeToggle);
    expect(button.getAttribute('aria-expanded')).toBe('true');
  }));

  it('makes Reportes landing navigable while its ten backend children remain independently expandable', fakeAsync(() => {
    menuSource.next([reportsMenu]);
    fixture.detectChanges();
    flushMicrotasks();
    fixture.detectChanges();

    const reportLink = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="70"]'
    ) as HTMLAnchorElement;
    const reportToggle = fixture.nativeElement.querySelector(
      'button[data-menu-toggle-id="70"]'
    ) as HTMLButtonElement;

    expect(component.navigableParentIds.has(70)).toBeTrue();
    expect(reportLink).not.toBeNull();
    expect(reportToggle).not.toBeNull();

    reportToggle.click();
    fixture.detectChanges();

    const reportChildren = fixture.nativeElement.querySelectorAll(
      '#submenu-70 a[data-menu-item-id]'
    ) as NodeListOf<HTMLAnchorElement>;
    expect(reportChildren.length).toBe(10);
    expect(Array.from(reportChildren).map((child) => child.textContent?.trim())).toEqual([
      'Transacciones enviadas',
      'Transacciones recibidas',
      'Devoluciones',
      'Rechazos',
      'Archivos NACHA-M',
      'Ciclos',
      'Conciliación',
      'Trazabilidad ACH',
      'Histórico',
      'Auditoría'
    ]);
    expect(fixture.nativeElement.textContent).not.toContain('Resumen de reportes');

    reportLink.click();
    tick();
    fixture.detectChanges();

    expect(router.url).toBe('/reports');
    expect(reportLink.getAttribute('aria-current')).toBe('page');
    expect(component.activeItemIds.has(70)).toBeTrue();

    const sent = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="701"]'
    ) as HTMLAnchorElement;
    sent.click();
    tick();
    fixture.detectChanges();

    expect(router.url).toBe('/reports/sent');
    expect(sent.getAttribute('aria-current')).toBe('page');
    expect(reportLink.getAttribute('aria-current')).toBeNull();
    expect(component.activeItemIds.has(70)).toBeTrue();
    expect(component.activeItemIds.has(701)).toBeTrue();
    expect(component.expandedItems.has(70)).toBeTrue();
  }));

  it('applies the same functional-parent model to Conexiones and preserves its canonical hierarchy', fakeAsync(() => {
    menuSource.next([connectionsMenu]);
    fixture.detectChanges();
    flushMicrotasks();
    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="50"]'
    ) as HTMLAnchorElement;
    const toggle = fixture.nativeElement.querySelector(
      'button[data-menu-toggle-id="50"]'
    ) as HTMLButtonElement;

    expect(component.navigableParentIds.has(50)).toBeTrue();
    expect(link).not.toBeNull();
    expect(toggle).not.toBeNull();

    toggle.click();
    fixture.detectChanges();

    const children = fixture.nativeElement.querySelectorAll(
      '#submenu-50 a[data-menu-item-id]'
    ) as NodeListOf<HTMLAnchorElement>;
    expect(Array.from(children).map((child) => child.textContent?.trim())).toEqual([
      'Contrato del servicio',
      'Relación de información',
      'Conexión entidad participante',
      'Configuración del intercambio de archivos'
    ]);
    expect(fixture.nativeElement.textContent).not.toContain('Centro de integración CFA');

    link.click();
    tick();
    fixture.detectChanges();
    expect(router.url).toBe('/integraciones');
  }));

  it('uses BrandingService for the private logo and centralized product fallback', () => {
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.brand-title')?.textContent).toContain('ACH Interbank');
    expect(host.querySelector('.brand img')).not.toBeNull();
  });

  it('navigates from the administrative menu to operational restrictions', fakeAsync(() => {
    fixture.detectChanges();
    flushMicrotasks();
    fixture.detectChanges();

    const catalogsLink = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="3"]'
    ) as HTMLAnchorElement;
    const catalogsToggle = fixture.nativeElement.querySelector(
      'button[data-menu-toggle-id="3"]'
    ) as HTMLButtonElement;
    expect(catalogsLink).not.toBeNull();
    expect(catalogsToggle).not.toBeNull();

    catalogsToggle.click();
    fixture.detectChanges();

    const restrictions = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="31"]'
    ) as HTMLAnchorElement;
    expect(restrictions).not.toBeNull();
    expect(restrictions.textContent).toContain('Restricciones operacionales CFA');

    restrictions.click();
    tick();
    fixture.detectChanges();

    expect(router.url).toBe('/operational-restrictions');
  }));

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

  it('keeps the desktop navigation recoverable through repeated compact transitions', () => {
    fixture.detectChanges();
    const toggle = menuToggle();

    expect(toggle.getAttribute('aria-expanded')).toBe('true');
    const sidenav = fixture.nativeElement.querySelector('.primary-sidenav') as HTMLElement;

    for (let cycle = 0; cycle < 10; cycle += 1) {
      toggle.click();
      fixture.detectChanges();

      const compact = cycle % 2 === 0;
      expect(component.isDesktopCompact).withContext(`ciclo ${cycle + 1}`).toBe(compact);
      expect(sidenav.classList.contains('compact')).withContext(`ciclo ${cycle + 1}`).toBe(compact);
      expect(toggle.getAttribute('aria-expanded'))
        .withContext(`ciclo ${cycle + 1}`)
        .toBe(compact ? 'false' : 'true');
      expect(toggle.getAttribute('aria-label'))
        .withContext(`ciclo ${cycle + 1}`)
        .toBe(compact ? 'Expandir navegación' : 'Contraer navegación');
    }

    toggle.click();
    fixture.detectChanges();

    expect(component.isDesktopCompact).toBeTrue();
    expect(toggle.getAttribute('aria-controls')).toBe('primary-navigation');
    expect(component.navigationToggleLabel).toBe('Expandir navegación');
    expect(
      sidenav
        .querySelector('a[data-menu-item-id="1"] app-ui-icon')
        ?.getAttribute('data-icon-key')
    ).toBe('dashboard');

    toggle.click();
    fixture.detectChanges();

    expect(component.isDesktopCompact).toBeFalse();
  });

  it('keeps desktop compact navigation functional and expands it before opening a group', fakeAsync(() => {
    fixture.detectChanges();
    component.toggleNavigation();
    fixture.detectChanges();

    const dashboard = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="1"]'
    ) as HTMLAnchorElement;
    dashboard.click();
    tick();
    fixture.detectChanges();

    expect(router.url).toBe('/dashboard');
    expect(component.isDesktopCompact).toBeTrue();

    void router.navigateByUrl('/transactions/cycles/detail');
    tick();
    fixture.detectChanges();
    expect(component.expandedItems.has(2)).toBeTrue();

    const operation = fixture.nativeElement.querySelector(
      'button[data-menu-item-id="2"]'
    ) as HTMLButtonElement;
    operation.click();
    fixture.detectChanges();

    expect(component.isDesktopCompact).toBeFalse();
    expect(component.expandedItems.has(2)).toBeTrue();
  }));

  it('keeps a mobile submenu open for touch and closes the drawer after navigation', fakeAsync(() => {
    setMobile(true);
    fixture.detectChanges();

    menuToggle().click();
    fixture.detectChanges();
    tick();

    expect(component.isMobileDrawerOpen).toBeTrue();

    const operation = fixture.nativeElement.querySelector(
      'button[data-menu-item-id="2"]'
    ) as HTMLButtonElement;
    operation.click();
    fixture.detectChanges();

    expect(component.isMobileDrawerOpen).toBeTrue();
    expect(operation.getAttribute('aria-expanded')).toBe('true');

    const audit = fixture.nativeElement.querySelector(
      'a[data-menu-item-id="22"]'
    ) as HTMLAnchorElement;
    audit.click();
    tick();
    fixture.detectChanges();

    expect(router.url).toBe('/audit-logs');
    expect(component.isMobileDrawerOpen).toBeFalse();
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

    expect(component.isMobileDrawerOpen).toBeFalse();

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

    expect(component.isMobileDrawerOpen).toBeFalse();
  }));

  it('closes the mobile overlay with Escape after a desktop-to-mobile transition', fakeAsync(() => {
    fixture.detectChanges();
    setMobile(true);
    fixture.detectChanges();

    menuToggle().click();
    fixture.detectChanges();
    tick();
    expect(component.isMobileDrawerOpen).toBeTrue();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    flushMicrotasks();
    fixture.detectChanges();

    expect(component.isMobileDrawerOpen).toBeFalse();
    expect(document.activeElement).toBe(menuToggle());
  }));

  it('keeps desktop compact state independent across desktop, mobile and desktop', () => {
    fixture.detectChanges();
    const sidenav = fixture.nativeElement.querySelector('mat-sidenav') as HTMLElement;

    component.toggleNavigation();
    fixture.detectChanges();

    expect(component.isMobileViewport).toBeFalse();
    expect(component.isDesktopCompact).toBeTrue();
    expect(sidenav.classList.contains('mat-drawer-side')).toBeTrue();

    setMobile(true);
    fixture.detectChanges();

    expect(component.isMobileViewport).toBeTrue();
    expect(component.isDesktopCompact).toBeTrue();
    expect(component.isMobileDrawerOpen).toBeFalse();
    expect(sidenav.classList.contains('mat-drawer-over')).toBeTrue();

    component.toggleNavigation();
    fixture.detectChanges();
    expect(component.isMobileDrawerOpen).toBeTrue();

    setMobile(false);
    fixture.detectChanges();

    expect(component.isMobileViewport).toBeFalse();
    expect(component.isMobileDrawerOpen).toBeFalse();
    expect(component.isDesktopCompact).toBeTrue();
    expect(sidenav.classList.contains('mat-drawer-side')).toBeTrue();

    component.toggleNavigation();
    fixture.detectChanges();
    expect(component.isDesktopCompact).toBeFalse();
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

  it('uses the canonical backend label for the visible and document title on Reportes', fakeAsync(() => {
    menuSource.next([reportsMenu]);
    fixture.detectChanges();
    flushMicrotasks();

    router.navigateByUrl('/reports?x=1#section');
    tick();
    fixture.detectChanges();

    expect(component.pageTitle).toBe('Reportes');
    expect(fixture.nativeElement.querySelector('.page-title')?.textContent.trim()).toBe('Reportes');
    expect(document.title).toBe('ACH Interbank - Reportes');
  }));

});
