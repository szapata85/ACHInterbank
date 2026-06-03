import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaConfigProfileReadModel } from '../models/nacha-config-admin.models';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigRecordsPageComponent } from './nacha-config-records-page.component';
import { NachaConfigVariantsFieldsPageComponent } from './nacha-config-variants-fields-page.component';

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

describe('NACHA Config official read-only pages', () => {
  afterEach(() => TestBed.resetTestingModule());

  async function createVariantsFixture(options?: { error?: boolean; profiles?: NachaConfigProfileReadModel[] }) {
    const query = jasmine.createSpyObj<NachaConfigQueryService>('NachaConfigQueryService', ['perfilesReadOnly']);
    query.perfilesReadOnly.and.returnValue(options?.error ? throwError(() => new Error('api')) : of(options?.profiles ?? [profile]));

    await TestBed.configureTestingModule({
      imports: [NachaConfigVariantsFieldsPageComponent],
      providers: [
        { provide: NachaConfigQueryService, useValue: query },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['error']) },
        { provide: Router, useValue: jasmine.createSpyObj<Router>('Router', ['navigate']) },
        { provide: ActivatedRoute, useValue: { snapshot: {}, params: of({}) } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(NachaConfigVariantsFieldsPageComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, query };
  }

  async function createRecordsFixture(options?: { error?: boolean; profiles?: NachaConfigProfileReadModel[] }) {
    const query = jasmine.createSpyObj<NachaConfigQueryService>('NachaConfigQueryService', ['perfilesReadOnly']);
    query.perfilesReadOnly.and.returnValue(options?.error ? throwError(() => new Error('api')) : of(options?.profiles ?? [profile]));

    await TestBed.configureTestingModule({
      imports: [NachaConfigRecordsPageComponent],
      providers: [
        { provide: NachaConfigQueryService, useValue: query },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['error']) },
        { provide: Router, useValue: jasmine.createSpyObj<Router>('Router', ['navigate']) },
        { provide: ActivatedRoute, useValue: { snapshot: {}, params: of({}) } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(NachaConfigRecordsPageComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, query };
  }

  it('VariantsFields_ShouldLoadOfficialProfiles', async () => {
    const { fixture, component, query } = await createVariantsFixture();

    expect(query.perfilesReadOnly).toHaveBeenCalled();
    expect(component.profiles.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('NACHA Config - Variants y Fields');
    expect(fixture.nativeElement.textContent).toContain('nacha-config profiles');
    expect(fixture.nativeElement.textContent).toContain('CENIT-OUT-220');
    expect(component.profiles[0].status).toBe('Published');
    expect(component.profiles[0].version).toBe('1.0');
    expect(fixture.nativeElement.textContent).not.toContain('Crear');
    expect(fixture.nativeElement.textContent).not.toContain('Editar');
    expect(fixture.nativeElement.textContent).not.toContain('Guardar');
    expect(fixture.nativeElement.textContent).not.toContain('Eliminar');
  });

  it('Records_ShouldLoadOfficialProfilesAndRecords', async () => {
    const { fixture, component, query } = await createRecordsFixture();

    expect(query.perfilesReadOnly).toHaveBeenCalled();
    expect(component.profiles.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('NACHA Config - Records');
    expect(fixture.nativeElement.textContent).toContain('nacha-config profiles');
    expect(fixture.nativeElement.textContent).toContain('1, 5, 6, 7, 8, 9');
    expect(fixture.nativeElement.textContent).not.toContain('Crear');
    expect(fixture.nativeElement.textContent).not.toContain('Editar');
    expect(fixture.nativeElement.textContent).not.toContain('Guardar');
    expect(fixture.nativeElement.textContent).not.toContain('Eliminar');
  });

  it('VariantsFields_ShouldShowClearEmptyState', async () => {
    const { fixture, component } = await createVariantsFixture({ profiles: [] });

    expect(component.profiles).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('Sin nacha-config profiles');
  });

  it('Records_ShouldShowClearEmptyState', async () => {
    const { fixture, component } = await createRecordsFixture({ profiles: [] });

    expect(component.profiles).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('Sin records oficiales');
  });

  it('OfficialReadOnlyPages_ShouldUseNachaConfigQueryServiceOnly', async () => {
    const variants = await createVariantsFixture();
    TestBed.resetTestingModule();
    const records = await createRecordsFixture();

    expect(variants.query.perfilesReadOnly).toHaveBeenCalledTimes(1);
    expect(records.query.perfilesReadOnly).toHaveBeenCalledTimes(1);
  });
});
