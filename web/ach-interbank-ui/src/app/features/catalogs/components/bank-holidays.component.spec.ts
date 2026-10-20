import { OverlayContainer } from '@angular/cdk/overlay';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Subject, of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import {
  BankHoliday,
  parseBankHolidayLocalDate,
  toBankHolidayDateOnly
} from '../models/bank-holiday.model';
import { BankHolidaysAdminService } from '../services/bank-holidays-admin.service';
import { BankHolidaysComponent } from './bank-holidays.component';

describe('BankHolidaysComponent', () => {
  let fixture: ComponentFixture<BankHolidaysComponent>;
  let component: BankHolidaysComponent;
  let service: jasmine.SpyObj<BankHolidaysAdminService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let overlayElement: HTMLElement;

  beforeEach(async () => {
    service = jasmine.createSpyObj<BankHolidaysAdminService>(
      'BankHolidaysAdminService',
      ['list', 'create', 'update', 'delete']
    );
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);
    service.list.and.returnValue(of([]));
    service.create.and.callFake((payload) => of(payload));
    service.update.and.callFake((payload) => of(payload));
    service.delete.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [BankHolidaysComponent, NoopAnimationsModule],
      providers: [
        { provide: BankHolidaysAdminService, useValue: service },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BankHolidaysComponent);
    component = fixture.componentInstance;
    overlayElement = TestBed.inject(OverlayContainer).getContainerElement();
    fixture.detectChanges();
  });

  afterEach(() => {
    overlayElement.replaceChildren();
    TestBed.resetTestingModule();
  });

  it('loads the current year and renders the empty state', () => {
    const currentYear = new Date().getFullYear();

    expect(service.list).toHaveBeenCalledOnceWith(currentYear);
    expect(component.lastLoadedYear).toBe(currentYear);
    expect(component.loading).toBeFalse();
    expect(component.hasSearched).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain(`Sin festivos para ${currentYear}`);
  });

  it('sends the selected year to the real filter and rejects values outside its limits', () => {
    service.list.calls.reset();
    component.filterForm.controls.year.setValue(2032);

    component.search();

    expect(service.list).toHaveBeenCalledOnceWith(2032);

    service.list.calls.reset();
    component.filterForm.controls.year.setValue(1899);
    component.search();

    expect(service.list).not.toHaveBeenCalled();
    expect(component.filterForm.controls.year.touched).toBeTrue();
  });

  it('exposes loading, error and retry states without discarding the selected year', () => {
    const pending = new Subject<BankHoliday[]>();
    service.list.and.returnValue(pending);

    component.load(2028);
    expect(component.loading).toBeTrue();
    expect(component.lastLoadedYear).toBe(2028);

    pending.error(new Error('network'));
    fixture.detectChanges();

    expect(component.loading).toBeFalse();
    expect(component.loadError).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('No fue posible cargar los festivos');
  });

  it('keeps DateOnly values on the same local calendar day at year boundaries', () => {
    const firstDay = parseBankHolidayLocalDate('2026-01-01T00:00:00Z');
    const lastDay = parseBankHolidayLocalDate('2026-12-31T23:59:59Z');

    expect(firstDay).not.toBeNull();
    expect(firstDay!.getFullYear()).toBe(2026);
    expect(firstDay!.getMonth()).toBe(0);
    expect(firstDay!.getDate()).toBe(1);
    expect(lastDay).not.toBeNull();
    expect(lastDay!.getFullYear()).toBe(2026);
    expect(lastDay!.getMonth()).toBe(11);
    expect(lastDay!.getDate()).toBe(31);
    expect(toBankHolidayDateOnly(firstDay)).toBe('2026-01-01');
    expect(toBankHolidayDateOnly(lastDay)).toBe('2026-12-31');
  });

  it('renders the 19 legal holidays for 2026 including the Chiquinquira transfer', () => {
    const legalHolidays = Array.from({ length: 19 }, (_, index) => holiday({
      id: index + 1,
      date: `2026-01-${`${index + 1}`.padStart(2, '0')}`,
      isSystemGenerated: true,
      ruleKind: 'Fixed'
    }));
    legalHolidays[10] = holiday({
      id: 11,
      date: '2026-07-13',
      commemorativeDate: '2026-07-09',
      description: 'Día de Nuestra Señora del Rosario de Chiquinquirá',
      isSystemGenerated: true,
      ruleKind: 'ChiquinquiraEmiliani',
      legalOrigin: 'Ley 2578 de 2026 y Ley 51 de 1983'
    });
    service.list.and.returnValue(of(legalHolidays));

    component.load(2026);
    fixture.detectChanges();

    expect(component.holidays.length).toBe(19);
    expect(fixture.nativeElement.textContent).toContain('13 de julio de 2026');
    expect(fixture.nativeElement.textContent).toContain('9 de julio de 2026');
    expect(fixture.nativeElement.textContent).toContain('Ley Emiliani');
  });

  it('protects legal records generated by the system from editing and deletion', () => {
    const generated = holiday({ isSystemGenerated: true, ruleKind: 'Fixed' });

    component.startEdit(generated);
    component.remove(generated);

    expect(component.showForm).toBeFalse();
    expect(service.delete).not.toHaveBeenCalled();
  });

  it('serializes a local day change without converting through UTC', () => {
    const localDate = new Date(2026, 11, 31, 23, 30);
    localDate.setDate(localDate.getDate() + 1);

    expect(toBankHolidayDateOnly(localDate)).toBe('2027-01-01');
    expect(parseBankHolidayLocalDate('2026-02-30')).toBeNull();
  });

  it('populates the datepicker from an API date without a UTC day shift', () => {
    component.startEdit(holiday({ date: '2026-01-01T00:00:00Z' }));

    const selected = component.form.controls.date.value;
    expect(selected).not.toBeNull();
    expect(selected!.getFullYear()).toBe(2026);
    expect(selected!.getMonth()).toBe(0);
    expect(selected!.getDate()).toBe(1);
  });

  it('marks invalid fields and does not submit an incomplete holiday', () => {
    component.startCreate();
    component.save();
    fixture.detectChanges();

    expect(service.create).not.toHaveBeenCalled();
    expect(component.form.controls.date.touched).toBeTrue();
    expect(component.form.controls.description.touched).toBeTrue();
    expect(fixture.nativeElement.querySelector('mat-datepicker-toggle')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('La fecha es obligatoria.');
  });

  it('creates with a DateOnly payload, prevents duplicate submission and reports success', () => {
    const pending = new Subject<BankHoliday>();
    service.create.and.returnValue(pending);
    component.startCreate();
    component.form.setValue({
      date: new Date(2026, 0, 1, 18, 45),
      description: ' Año nuevo ',
      countryCode: ' co '
    });

    component.save();
    component.save();

    expect(service.create).toHaveBeenCalledTimes(1);
    expect(service.create.calls.mostRecent().args[0]).toEqual({
      id: 0,
      date: '2026-01-01',
      description: 'Año nuevo',
      countryCode: 'CO'
    });
    expect(component.saving).toBeTrue();

    pending.next(holiday());
    pending.complete();

    expect(component.saving).toBeFalse();
    expect(component.showForm).toBeFalse();
    expect(component.successMessage).toBe('Festivo creado correctamente.');
    expect(notifications.success).toHaveBeenCalledOnceWith('Festivo creado correctamente.');
  });

  it('updates the existing DTO and keeps the form open when the request fails', () => {
    service.update.and.returnValue(throwError(() => new Error('api')));
    component.startEdit(holiday());
    component.form.controls.description.setValue('Año nuevo actualizado');

    component.save();

    expect(service.update).toHaveBeenCalledOnceWith({
      id: 7,
      date: '2026-01-01',
      description: 'Año nuevo actualizado',
      countryCode: 'CO'
    });
    expect(component.saving).toBeFalse();
    expect(component.showForm).toBeTrue();
    expect(component.operationError).toContain('No fue posible actualizar');
    expect(notifications.error).toHaveBeenCalled();
  });

  it('requires explicit confirmation and prevents duplicate deletion while pending', fakeAsync(() => {
    component.remove(holiday());
    fixture.detectChanges();
    tick();

    expect(overlayElement.textContent).toContain('Esta acción no se puede deshacer.');
    overlayButton('Cancelar').click();
    tick();
    expect(service.delete).not.toHaveBeenCalled();

    const pending = new Subject<void>();
    service.delete.and.returnValue(pending);
    component.remove(holiday());
    fixture.detectChanges();
    tick();
    overlayButton('Eliminar').click();
    tick();
    component.remove(holiday());

    expect(service.delete).toHaveBeenCalledOnceWith(7);
    expect(component.saving).toBeTrue();

    pending.next();
    pending.complete();
    tick();

    expect(component.saving).toBeFalse();
    expect(component.successMessage).toBe('Festivo eliminado correctamente.');
    expect(notifications.success).toHaveBeenCalledWith('Festivo eliminado correctamente.');
  }));

  function overlayButton(label: string): HTMLButtonElement {
    const button = Array.from(overlayElement.querySelectorAll('button'))
      .find((candidate) => candidate.textContent?.trim() === label);
    expect(button).withContext(`No se encontró el botón ${label} en el diálogo`).toBeDefined();
    return button!;
  }
});

function holiday(overrides: Partial<BankHoliday> = {}): BankHoliday {
  return {
    id: 7,
    date: '2026-01-01',
    description: 'Año nuevo',
    countryCode: 'CO',
    ...overrides
  };
}
