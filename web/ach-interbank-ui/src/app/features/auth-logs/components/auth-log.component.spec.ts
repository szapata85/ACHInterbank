import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Subject, of, throwError } from 'rxjs';
import { AuthLogEntry, PagedResponse } from '../models/auth-log.model';
import { AuthLogService } from '../services/auth-log.service';
import { AuthLogComponent } from './auth-log.component';

describe('AuthLogComponent', () => {
  let fixture: ComponentFixture<AuthLogComponent>;
  let component: AuthLogComponent;
  let service: jasmine.SpyObj<AuthLogService>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<AuthLogService>('AuthLogService', ['search']);

    await TestBed.configureTestingModule({
      imports: [AuthLogComponent],
      providers: [
        provideNoopAnimations(),
        { provide: AuthLogService, useValue: service }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AuthLogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates typed reactive filters and accessible Material datepickers', () => {
    expect(component.filterForm.getRawValue()).toEqual({
      startDate: null,
      endDate: null,
      username: '',
      success: ''
    });
    expect(fixture.nativeElement.querySelectorAll('mat-form-field').length).toBe(4);
    const datepickerButtons = Array.from(
      fixture.nativeElement.querySelectorAll('mat-datepicker-toggle button')
    ) as HTMLButtonElement[];
    expect(datepickerButtons.map((button) => button.getAttribute('aria-label'))).toEqual([
      'Abrir calendario de fecha inicial',
      'Abrir calendario de fecha final'
    ]);
    expect(fixture.nativeElement.querySelector('table[mat-table]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Prepara tu consulta');
  });

  it('rejects an inverted date range without invoking the service', () => {
    component.filterForm.patchValue({
      startDate: new Date(2026, 6, 30),
      endDate: new Date(2026, 6, 1)
    });

    component.search();
    fixture.detectChanges();

    expect(service.search).not.toHaveBeenCalled();
    expect(component.showDateRangeError).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain(
      'La fecha inicial no puede ser posterior'
    );
  });

  it('preserves boolean filter semantics and sanitizes failed-login details', () => {
    const syntheticJwt =
      'eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJmaWN0aWNpbyJ9.firmaSintetica123';
    service.search.and.returnValue(
      of({
        items: [
          authEntry({
            success: false,
            failureReason: `password=ClaveFicticia token=${syntheticJwt}`
          })
        ],
        total: 1,
        page: 1,
        pageSize: 20
      })
    );
    component.filterForm.setValue({
      startDate: new Date(2026, 6, 1, 23, 30),
      endDate: new Date(2026, 6, 30, 0, 15),
      username: '  usuario.prueba  ',
      success: 'false'
    });

    component.search();
    fixture.detectChanges();

    expect(service.search).toHaveBeenCalledOnceWith({
      startDate: '2026-07-01',
      endDate: '2026-07-30',
      username: 'usuario.prueba',
      success: false,
      page: 1,
      pageSize: 20
    });
    expect(component.rows[0].resultDisplay).toBe('Fallido');
    expect(component.rows[0].resultIcon).toBe('cancel');
    expect(component.rows[0].failureReasonDisplay).toContain('[REDACTADO]');
    expect(component.rows[0].failureReasonDisplay).not.toContain('ClaveFicticia');
    expect(component.rows[0].failureReasonDisplay).not.toContain(syntheticJwt);
    const chipSet = fixture.nativeElement.querySelector('mat-chip-set') as HTMLElement;
    expect(chipSet.getAttribute('aria-label')).toBe('Resultado: Fallido');
  });

  it('labels successful results with text and icon instead of color alone', () => {
    service.search.and.returnValue(
      of({
        items: [authEntry({ success: true, failureReason: 'No aplica' })],
        total: 1,
        page: 1,
        pageSize: 20
      })
    );

    component.search();
    fixture.detectChanges();

    expect(component.rows[0].resultDisplay).toBe('Exitoso');
    expect(component.rows[0].resultIcon).toBe('check_circle');
    expect(component.rows[0].failureReasonDisplay).toBe('No aplica');
    expect(fixture.nativeElement.querySelector('mat-chip').textContent).toContain(
      'Exitoso'
    );
  });

  it('keeps the loading state and prevents a duplicate in-flight search', () => {
    const response$ = new Subject<PagedResponse<AuthLogEntry>>();
    service.search.and.returnValue(response$);

    component.search();
    component.search();

    expect(service.search).toHaveBeenCalledTimes(1);
    expect(component.loading).toBeTrue();

    response$.next({ items: [], total: 0, page: 1, pageSize: 20 });
    response$.complete();

    expect(component.loading).toBeFalse();
  });

  it('handles service errors without exposing the technical error', () => {
    service.search.and.returnValue(
      throwError(() => new Error('Authorization: Bearer valor-sintetico'))
    );

    component.search();
    fixture.detectChanges();

    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toContain('No fue posible cargar');
    expect(component.errorMessage).not.toContain('valor-sintetico');
    expect(
      (fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement).textContent
    ).toContain('No se pudo completar la consulta');
  });

  it('uses the paginator event to request the correct server page and size', () => {
    service.search.and.returnValue(
      of({ items: [], total: 90, page: 2, pageSize: 50 })
    );

    component.onPageChange({
      pageIndex: 1,
      previousPageIndex: 0,
      pageSize: 50,
      length: 90
    });

    expect(service.search).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ page: 2, pageSize: 50 })
    );
  });

  it('sorts the visible page and clears all local query state', () => {
    service.search.and.returnValue(
      of({
        items: [
          authEntry({ id: '2', username: 'Zeta' }),
          authEntry({ id: '1', username: 'Álvaro' })
        ],
        total: 2,
        page: 1,
        pageSize: 20
      })
    );
    component.filterForm.patchValue({ username: 'usuario' });
    component.search();

    component.onSortChange({ active: 'username', direction: 'asc' });

    expect(component.rows.map((row) => row.id)).toEqual(['1', '2']);

    component.clear();

    expect(component.filterForm.getRawValue()).toEqual({
      startDate: null,
      endDate: null,
      username: '',
      success: ''
    });
    expect(component.rows).toEqual([]);
    expect(component.total).toBe(0);
    expect(component.hasSearched).toBeFalse();
    expect(component.errorMessage).toBeNull();
  });

  function authEntry(overrides: Partial<AuthLogEntry> = {}): AuthLogEntry {
    return {
      id: 'auth-1',
      username: 'usuario.prueba',
      success: true,
      failureReason: null,
      ipAddress: '127.0.0.1',
      userAgent: 'Navegador de prueba',
      loggedAt: '2026-07-30T15:00:00Z',
      ...overrides
    };
  }
});
