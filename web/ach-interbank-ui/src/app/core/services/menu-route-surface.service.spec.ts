import { Component, NgModule } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, RouterModule, Routes } from '@angular/router';
import { MenuRouteSurfaceService } from './menu-route-surface.service';

@Component({ standalone: true, template: '<p>Functional surface</p>' })
class FunctionalSurfaceComponent {}

const FUNCTIONAL_LAZY_ROUTES: Routes = [
  { path: '', component: FunctionalSurfaceComponent },
  { path: 'child', component: FunctionalSurfaceComponent }
];

const STRUCTURAL_LAZY_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'child' },
  { path: 'child', component: FunctionalSurfaceComponent }
];

@NgModule({
  imports: [RouterModule.forChild(FUNCTIONAL_LAZY_ROUTES)]
})
class FunctionalLazyModule {}

@NgModule({
  imports: [RouterModule.forChild(STRUCTURAL_LAZY_ROUTES)]
})
class StructuralLazyModule {}

describe('MenuRouteSurfaceService', () => {
  let service: MenuRouteSurfaceService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          {
            path: '',
            children: [
              { path: 'direct', component: FunctionalSurfaceComponent },
              { path: 'redirect', pathMatch: 'full', redirectTo: 'direct' },
              {
                path: 'pathless-functional',
                children: [{ path: '', component: FunctionalSurfaceComponent }]
              },
              {
                path: 'pathless-redirect',
                children: [
                  { path: '', pathMatch: 'full', redirectTo: 'child' },
                  { path: 'child', component: FunctionalSurfaceComponent }
                ]
              },
              {
                path: 'lazy-routes-functional',
                loadChildren: () => Promise.resolve(FUNCTIONAL_LAZY_ROUTES)
              },
              {
                path: 'lazy-routes-structural',
                loadChildren: () => Promise.resolve(STRUCTURAL_LAZY_ROUTES)
              },
              {
                path: 'lazy-module-functional',
                loadChildren: () => Promise.resolve(FunctionalLazyModule)
              },
              {
                path: 'lazy-module-structural',
                loadChildren: () => Promise.resolve(StructuralLazyModule)
              },
              {
                path: 'future-parent',
                loadChildren: () => Promise.resolve([
                  { path: '', loadComponent: () => Promise.resolve(FunctionalSurfaceComponent) },
                  { path: 'a', component: FunctionalSurfaceComponent },
                  { path: 'b', component: FunctionalSurfaceComponent }
                ])
              }
            ]
          }
        ])
      ]
    });

    service = TestBed.inject(MenuRouteSurfaceService);
  });

  it('classifies direct components and pathless default components as functional', async () => {
    expect(await service.hasFunctionalSurface('/direct')).toBeTrue();
    expect(await service.hasFunctionalSurface('/pathless-functional')).toBeTrue();
  });

  it('classifies redirects and missing routes as structural', async () => {
    expect(await service.hasFunctionalSurface('/redirect')).toBeFalse();
    expect(await service.hasFunctionalSurface('/pathless-redirect')).toBeFalse();
    expect(await service.hasFunctionalSurface('/missing-parent')).toBeFalse();
  });

  it('inspects lazy route arrays without a hardcoded menu-route list', async () => {
    expect(await service.hasFunctionalSurface('/lazy-routes-functional')).toBeTrue();
    expect(await service.hasFunctionalSurface('/lazy-routes-structural')).toBeFalse();
  });

  it('inspects lazy NgModule route configuration generically', async () => {
    expect(await service.hasFunctionalSurface('/lazy-module-functional')).toBeTrue();
    expect(await service.hasFunctionalSurface('/lazy-module-structural')).toBeFalse();
  });

  it('automatically recognizes a future registered parent with its own lazy functional landing', async () => {
    expect(await service.hasFunctionalSurface('/future-parent')).toBeTrue();
  });

  it('does not treat malformed or empty menu routes as functional', async () => {
    expect(await service.hasFunctionalSurface('http://[invalid')).toBeFalse();
    expect(await service.hasFunctionalSurface('')).toBeFalse();
  });
});
