import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaConfigProfileReadModel } from '../../nacha-config-admin/models/nacha-config-admin.models';
import { NachaConfigQueryService } from '../../nacha-config-admin/services/nacha-config-query.service';
import { NachaLayoutsComponent } from './nacha-layouts.component';
import { NachaRecordDefinitionsComponent } from './nacha-record-definitions.component';

const profile: NachaConfigProfileReadModel = {
  profileId: 10,
  profileCode: 'CENIT-OUT-220',
  profileName: 'CENIT salida 220',
  clearingHouseCode: 'CENIT',
  flowType: 'Outgoing',
  status: 'Published',
  version: '1.0',
  isPublished: true,
  isCurrent: true,
  effectiveFrom: '2026-01-01T00:00:00Z',
  effectiveTo: null,
  layoutVariantCount: 6,
  fieldCount: 42,
  recordTypes: ['1', '5', '6', '7', '8', '9'],
  isOfficialModel: true,
  legacyDeprecated: true
};

describe('NACHA-M compatibility routes official config', () => {
  afterEach(() => TestBed.resetTestingModule());

  async function createLayoutsFixture(options?: { error?: boolean; profiles?: NachaConfigProfileReadModel[] }) {
    const query = jasmine.createSpyObj<NachaConfigQueryService>('NachaConfigQueryService', ['perfilesReadOnly']);
    query.perfilesReadOnly.and.returnValue(options?.error ? throwError(() => new Error('api')) : of(options?.profiles ?? [profile]));

    await TestBed.configureTestingModule({
      imports: [NachaLayoutsComponent],
      providers: [
        { provide: NachaConfigQueryService, useValue: query },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['error']) },
        { provide: Router, useValue: jasmine.createSpyObj<Router>('Router', ['navigate']) },
        { provide: ActivatedRoute, useValue: { snapshot: {}, params: of({}) } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(NachaLayoutsComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, query };
  }

  async function createDefinitionsFixture(options?: { error?: boolean; profiles?: NachaConfigProfileReadModel[] }) {
    const query = jasmine.createSpyObj<NachaConfigQueryService>('NachaConfigQueryService', ['perfilesReadOnly']);
    query.perfilesReadOnly.and.returnValue(options?.error ? throwError(() => new Error('api')) : of(options?.profiles ?? [profile]));

    await TestBed.configureTestingModule({
      imports: [NachaRecordDefinitionsComponent],
      providers: [
        { provide: NachaConfigQueryService, useValue: query },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['error']) },
        { provide: Router, useValue: jasmine.createSpyObj<Router>('Router', ['navigate']) },
        { provide: ActivatedRoute, useValue: { snapshot: {}, params: of({}) } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(NachaRecordDefinitionsComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, query };
  }

  it('LayoutsRoute_ShouldLoadOfficialProfiles', async () => {
    const { fixture, component, query } = await createLayoutsFixture();

    expect(query.perfilesReadOnly).toHaveBeenCalled();
    expect(component.profiles.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('NACHA Config - Variants y Fields');
    expect(fixture.nativeElement.textContent).toContain('nacha-config profiles');
    expect(fixture.nativeElement.textContent).toContain('CENIT-OUT-220');
    expect(fixture.nativeElement.textContent).toContain('CENIT');
    expect(fixture.nativeElement.textContent).toContain('Outgoing');
    expect(component.profiles[0].status).toBe('Published');
    expect(component.profiles[0].version).toBe('1.0');
    expect(fixture.nativeElement.textContent).not.toContain('Crear');
    expect(fixture.nativeElement.textContent).not.toContain('Editar');
    expect(fixture.nativeElement.textContent).not.toContain('Guardar');
    expect(fixture.nativeElement.textContent).not.toContain('Eliminar');
  });

  it('DefinitionsRoute_ShouldLoadOfficialProfilesAndRecords', async () => {
    const { fixture, component, query } = await createDefinitionsFixture();

    expect(query.perfilesReadOnly).toHaveBeenCalled();
    expect(component.profiles.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('NACHA Config - Records');
    expect(fixture.nativeElement.textContent).toContain('nacha-config profiles');
    expect(fixture.nativeElement.textContent).toContain('1, 5, 6, 7, 8, 9');
    expect(fixture.nativeElement.textContent).toContain('CENIT');
    expect(fixture.nativeElement.textContent).toContain('Outgoing');
    expect(fixture.nativeElement.textContent).toContain('Published');
    expect(fixture.nativeElement.textContent).toContain('1.0');
    expect(fixture.nativeElement.textContent).not.toContain('Administrar campos');
    expect(fixture.nativeElement.textContent).not.toContain('Guardar');
    expect(fixture.nativeElement.textContent).not.toContain('Eliminar');
  });

  it('LayoutsRoute_ShouldShowClearEmptyState', async () => {
    const { fixture, component } = await createLayoutsFixture({ profiles: [] });

    expect(component.profiles).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('Sin nacha-config profiles');
  });

  it('DefinitionsRoute_ShouldShowClearEmptyState', async () => {
    const { fixture, component } = await createDefinitionsFixture({ profiles: [] });

    expect(component.profiles).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('Sin records oficiales');
  });

  it('CompatibilityRoutes_ShouldUseNachaConfigQueryServiceOnly', async () => {
    const layouts = await createLayoutsFixture();
    TestBed.resetTestingModule();
    const definitions = await createDefinitionsFixture();

    expect(layouts.query.perfilesReadOnly).toHaveBeenCalledTimes(1);
    expect(definitions.query.perfilesReadOnly).toHaveBeenCalledTimes(1);
  });
});
