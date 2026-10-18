import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchCycleListComponent } from './ach-cycle-list.component';
import { AchCyclesApiService } from '../../../core/services/ach-cycles-api.service';
import { AchCycleSummary } from '../../../core/models/ach-cycle.model';
import { ClearingHousesApiService } from '../../../core/services/clearing-houses-api.service';

describe('AchCycleListComponent', () => {
  let fixture: ComponentFixture<AchCycleListComponent>;
  let component: AchCycleListComponent;
  let cyclesApi: jasmine.SpyObj<AchCyclesApiService>;
  let clearingHousesApi: jasmine.SpyObj<ClearingHousesApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let router: Router;

  const cycle: AchCycleSummary = {
    id: 'cycle-17',
    cycleName: 'Ciclo 1',
    clearingHouseId: 7,
    clearingHouseName: 'ACH Colombia',
    date: '2026-10-03',
    startTime: '08:15:00',
    endTime: '09:45:00',
    cutoffTime: '09:30:00',
    rescheduleOnHoliday: false,
    clearingHouseCycleConfigId: 3,
    operationalStatus: 'Active',
    status: 'active'
  };

  beforeEach(async () => {
    cyclesApi = jasmine.createSpyObj<AchCyclesApiService>('AchCyclesApiService', ['search']);
    cyclesApi.search.and.returnValue(of({ items: [], total: 0, page: 1, pageSize: 10 }));

    clearingHousesApi = jasmine.createSpyObj<ClearingHousesApiService>('ClearingHousesApiService', ['list']);
    clearingHousesApi.list.and.returnValue(of([
      { id: 7, code: 'ACHCOL', name: 'ACH Colombia' },
      { id: 9, code: 'CENIT', name: 'CENIT' }
    ]));

    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['error']);

    await TestBed.configureTestingModule({
      imports: [AchCycleListComponent],
      providers: [
        provideRouter([]),
        { provide: AchCyclesApiService, useValue: cyclesApi },
        { provide: ClearingHousesApiService, useValue: clearingHousesApi },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
  });

  it('carga cámaras y ciclos al iniciar y conserva el formateo de la grilla', () => {
    cyclesApi.search.and.returnValue(of({ items: [cycle], total: 1, page: 1, pageSize: 10 }));
    createFixture();

    expect(clearingHousesApi.list).toHaveBeenCalledTimes(1);
    expect(cyclesApi.search).toHaveBeenCalledWith(jasmine.objectContaining({ page: 1, pageSize: 10 }));
    const mapped = component.cycles[0] as AchCycleSummary & {
      startText: string;
      endText: string;
      statusText: string;
    };
    expect(mapped.id).toBe('cycle-17');
    expect(mapped.startText).toBe('08:15');
    expect(mapped.endText).toBe('09:45');
    expect(mapped.statusText).toBe('Activo');
    expect(component.total).toBe(1);
  });

  it('resuelve clearingHouseCode contra las cámaras antes de buscar', () => {
    createFixture(false);
    const route = (component as any).route as ActivatedRoute;
    spyOn(route.snapshot.queryParamMap, 'get').and.callFake((key: string) =>
      key === 'clearingHouseCode' ? 'ACHCOL' : null);

    fixture.detectChanges();

    expect(component.filterForm.controls.clearingHouseId.value).toBe(7);
    expect(cyclesApi.search).toHaveBeenCalledTimes(1);
    expect(cyclesApi.search).toHaveBeenCalledWith(jasmine.objectContaining({ clearingHouseId: 7, page: 1 }));
  });

  it('aplica filtros y reinicia la página a 1', () => {
    createFixture();
    cyclesApi.search.calls.reset();
    component.filterForm.patchValue({
      clearingHouseId: 9,
      startDate: '2026-10-01',
      endDate: '2026-10-31',
      page: 4,
      pageSize: 25
    });

    component.submit();

    expect(component.filterForm.controls.page.value).toBe(1);
    expect(cyclesApi.search).toHaveBeenCalledWith(jasmine.objectContaining({
      clearingHouseId: 9,
      startDate: '2026-10-01',
      endDate: '2026-10-31',
      page: 1,
      pageSize: 25
    }));
  });

  it('preserva la paginación page/pageSize', () => {
    createFixture();
    cyclesApi.search.calls.reset();
    component.filterForm.controls.pageSize.setValue(25);

    component.changePage(3);

    expect(cyclesApi.search).toHaveBeenCalledWith(jasmine.objectContaining({ page: 3, pageSize: 25 }));
  });

  it('no inventa un clearingHouseId y vuelve al listado de cámaras en reglas de procesamiento', () => {
    createFixture();

    expect(processingRulesLink().getAttribute('href')).toBe('/clearing-houses');
  });

  it('usa la ruta canónica de reglas cuando ya existe clearingHouseId', () => {
    createFixture();
    component.filterForm.controls.clearingHouseId.setValue(7);
    fixture.detectChanges();

    expect(processingRulesLink().getAttribute('href')).toBe('/clearing-houses/7/cycles');
  });

  it('conserva la navegación create/edit/export', () => {
    createFixture();
    const navigate = spyOn(router, 'navigate');

    component.create();
    component.edit(cycle);
    component.goToExport();

    expect(navigate.calls.allArgs()).toEqual([
      [['/ach-cycles/new']],
      [['/ach-cycles', 'cycle-17', 'edit']],
      [['/ach-cycles/nacha/export']]
    ]);
  });

  it('muestra el estado compartido de carga mientras search está pendiente', () => {
    const pending = new Subject<any>();
    cyclesApi.search.and.returnValue(pending);
    createFixture();

    expect(component.loading).toBeTrue();
    expect(fixture.nativeElement.querySelector('ui-estado-carga')).not.toBeNull();

    pending.next({ items: [], total: 0, page: 1, pageSize: 10 });
    pending.complete();
  });

  it('muestra el estado compartido de error y conserva la notificación actual', () => {
    cyclesApi.search.and.returnValue(throwError(() => new Error('fallo')));
    createFixture();

    expect(component.loadError).toBeTrue();
    expect(notifications.error).toHaveBeenCalledWith('No fue posible cargar los ciclos ACH');
    expect(fixture.nativeElement.querySelector('ui-estado-error')).not.toBeNull();
  });

  it('muestra el estado compartido vacío cuando la búsqueda no retorna ciclos', () => {
    createFixture();

    expect(component.cycles).toEqual([]);
    expect(fixture.nativeElement.querySelector('ui-estado-vacio')).not.toBeNull();
  });

  it('usa controles Material en filtros y acciones sin reemplazar app-table', () => {
    createFixture();

    expect(fixture.nativeElement.querySelector('mat-select')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('input[matinput]').length).toBe(2);
    expect(fixture.nativeElement.querySelector('app-table')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('ui-grilla-empresarial').length).toBeGreaterThan(0);
  });

  function createFixture(detectChanges = true): void {
    fixture = TestBed.createComponent(AchCycleListComponent);
    component = fixture.componentInstance;
    if (detectChanges) {
      fixture.detectChanges();
    }
  }

  function processingRulesLink(): HTMLAnchorElement {
    const links = Array.from(fixture.nativeElement.querySelectorAll('.cycle-tabs a')) as HTMLAnchorElement[];
    const link = links.find((candidate) => candidate.textContent?.includes('Reglas de procesamiento'));
    expect(link).withContext('No existe el enlace Reglas de procesamiento').toBeDefined();
    return link!;
  }
});
