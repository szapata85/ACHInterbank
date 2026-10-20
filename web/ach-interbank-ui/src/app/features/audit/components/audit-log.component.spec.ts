import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Subject, of, throwError } from 'rxjs';
import { AuditLogEntry, PagedResponse } from '../models/audit-log.model';
import { AuditLogService } from '../services/audit-log.service';
import { AuditLogComponent } from './audit-log.component';

describe('AuditLogComponent', () => {
  let fixture: ComponentFixture<AuditLogComponent>;
  let component: AuditLogComponent;
  let service: jasmine.SpyObj<AuditLogService>;

  beforeEach(async () => {
    service = jasmine.createSpyObj<AuditLogService>('AuditLogService', ['search']);

    await TestBed.configureTestingModule({
      imports: [AuditLogComponent],
      providers: [
        provideNoopAnimations(),
        { provide: AuditLogService, useValue: service }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AuditLogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates typed reactive filters and accessible Material datepickers', () => {
    expect(component.filterForm.getRawValue()).toEqual({
      startDate: null,
      endDate: null,
      changedBy: '',
      action: ''
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

  it('sends the existing filter DTO and maps a sanitized, accessible row', () => {
    service.search.and.returnValue(
      of({
        items: [
          auditEntry({
            action: 'Modified',
            changedFields: '["DisplayName","PasswordHash","DisplayName"]'
          })
        ],
        total: 1,
        page: 2,
        pageSize: 20
      })
    );
    component.filterForm.setValue({
      startDate: new Date(2026, 6, 1, 23, 30),
      endDate: new Date(2026, 6, 30, 0, 15),
      changedBy: '  operador.prueba  ',
      action: 'Modified'
    });

    component.search(2);
    fixture.detectChanges();

    expect(service.search).toHaveBeenCalledOnceWith({
      startDate: '2026-07-01',
      endDate: '2026-07-30',
      changedBy: 'operador.prueba',
      action: 'Modified',
      page: 2,
      pageSize: 20
    });
    expect(component.rows[0].actionDisplay).toBe('Modificado');
    expect(component.rows[0].actionIcon).toBe('edit');
    expect(component.rows[0].changedFieldsDisplay).toBe(
      'DisplayName, Campo sensible'
    );
    const chipSet = fixture.nativeElement.querySelector('mat-chip-set') as HTMLElement;
    expect(chipSet.getAttribute('aria-label')).toBe('Acción: Modificado');
  });

  it('keeps the loading state and prevents a duplicate in-flight search', () => {
    const response$ = new Subject<PagedResponse<AuditLogEntry>>();
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
      throwError(() => new Error('token=valor-sintetico-interno'))
    );

    component.search();
    fixture.detectChanges();

    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toContain('No fue posible cargar');
    expect(component.errorMessage).not.toContain('valor-sintetico-interno');
    expect(
      (fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement).textContent
    ).toContain('No se pudo completar la consulta');
  });

  it('uses the paginator event to request the correct server page and size', () => {
    service.search.and.returnValue(
      of({ items: [], total: 120, page: 3, pageSize: 50 })
    );

    component.onPageChange({
      pageIndex: 2,
      previousPageIndex: 1,
      pageSize: 50,
      length: 120
    });

    expect(service.search).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ page: 3, pageSize: 50 })
    );
  });

  it('sorts the visible server page without mutating the DTO values', () => {
    service.search.and.returnValue(
      of({
        items: [
          auditEntry({ id: '2', changedBy: 'Zeta' }),
          auditEntry({ id: '1', changedBy: 'Álvaro' })
        ],
        total: 2,
        page: 1,
        pageSize: 20
      })
    );
    component.search();

    component.onSortChange({ active: 'changedBy', direction: 'asc' });

    expect(component.rows.map((row) => row.id)).toEqual(['1', '2']);
    expect(component.rows.map((row) => row.changedBy)).toEqual(['Álvaro', 'Zeta']);
  });

  it('clears filters, results, paging and error state together', () => {
    service.search.and.returnValue(
      of({ items: [auditEntry()], total: 1, page: 1, pageSize: 20 })
    );
    component.filterForm.patchValue({ changedBy: 'operador' });
    component.search();
    component.errorMessage = 'Error previo';

    component.clear();

    expect(component.filterForm.getRawValue()).toEqual({
      startDate: null,
      endDate: null,
      changedBy: '',
      action: ''
    });
    expect(component.rows).toEqual([]);
    expect(component.total).toBe(0);
    expect(component.page).toBe(1);
    expect(component.hasSearched).toBeFalse();
    expect(component.errorMessage).toBeNull();
  });

  function auditEntry(
    overrides: Partial<AuditLogEntry> = {}
  ): AuditLogEntry {
    return {
      id: 'audit-1',
      entityName: 'Configuración',
      entityId: 'entity-1',
      action: 'Added',
      changedBy: 'operador',
      changedAt: '2026-07-30T15:00:00Z',
      changedFields: '["Name"]',
      ...overrides
    };
  }
});
