import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ClearingHousesService } from '../../clearing-houses/clearing-houses.service';
import { BankHolidaysAdminService } from '../services/bank-holidays-admin.service';
import { ClearingHouseSpecialDatesService } from '../services/clearing-house-special-dates.service';
import {
  ClearingHouseSpecialDatesComponent,
  ClearingHouseSpecialDatesLegacyRedirectComponent
} from './clearing-house-special-dates.component';

describe('ClearingHouseSpecialDatesComponent', () => {
  let fixture: ComponentFixture<ClearingHouseSpecialDatesComponent>;
  let component: ClearingHouseSpecialDatesComponent;
  let params$: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  let service: jasmine.SpyObj<ClearingHouseSpecialDatesService>;
  let houses: jasmine.SpyObj<ClearingHousesService>;
  let holidays: jasmine.SpyObj<BankHolidaysAdminService>;

  beforeEach(async () => {
    params$ = new BehaviorSubject(convertToParamMap({ id: '7' }));
    service = jasmine.createSpyObj('ClearingHouseSpecialDatesService', ['list', 'create', 'update', 'changeStatus']);
    houses = jasmine.createSpyObj('ClearingHousesService', ['get']);
    holidays = jasmine.createSpyObj('BankHolidaysAdminService', ['list']);
    houses.get.and.returnValue(of({ id: 7, code: 'ACHCOL', name: 'ACH Colombia', isActive: true } as any));
    service.list.and.returnValue(of([specialDate()]));
    service.create.and.returnValue(of(specialDate()));
    service.update.and.returnValue(of(specialDate()));
    service.changeStatus.and.returnValue(of({ ...specialDate(), isActive: false }));
    holidays.list.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [ClearingHouseSpecialDatesComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: ActivatedRoute, useValue: { paramMap: params$.asObservable() } },
        { provide: AuthService, useValue: { hasPermission: () => true } },
        { provide: ClearingHousesService, useValue: houses },
        { provide: ClearingHouseSpecialDatesService, useValue: service },
        { provide: BankHolidaysAdminService, useValue: holidays }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ClearingHouseSpecialDatesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the fixed camera and dates from the contextual route', () => {
    expect(houses.get).toHaveBeenCalledWith(7);
    expect(service.list).toHaveBeenCalledWith(new Date().getFullYear(), 7);
    expect(component.clearingHouse?.code).toBe('ACHCOL');
  });

  it('has no camera selector, imperative grid or legacy buttons', () => {
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('[formControlName="clearingHouseId"]')).toBeNull();
    expect(element.querySelector('select')).toBeNull();
    expect(element.querySelector('ui-grilla-empresarial')).toBeNull();
    expect(element.querySelector('.btn')).toBeNull();
    expect(element.querySelector('table[mat-table]')).not.toBeNull();
  });

  it('handles route changes and HTTP errors', () => {
    houses.get.and.returnValue(throwError(() => ({ status: 403 })));
    params$.next(convertToParamMap({ id: '8' }));
    fixture.detectChanges();
    expect(houses.get).toHaveBeenCalledWith(8);
    expect(component.error).toContain('permisos');
    expect(component.loading).toBeFalse();
  });

  it('validates weekend, bank holiday and duplicate dates without timezone shifts', () => {
    component.startCreate();
    component.form.setValue({ date: new Date(2026, 7, 1), description: 'Cierre sabatino' });
    component.save();
    expect(component.form.controls.date.hasError('weekendDate')).toBeTrue();

    holidays.list.and.returnValue(of([{ id: 1, date: '2027-08-03', name: 'Festivo' } as any]));
    component.form.controls.date.setValue(new Date(2027, 7, 3));
    component.save();
    expect(component.form.controls.date.hasError('bankHoliday')).toBeTrue();

    component.allItems = [{ ...specialDate(), date: '2027-08-04' }];
    component.form.controls.date.setValue(new Date(2027, 7, 4));
    component.save();
    expect(component.form.controls.date.hasError('duplicateDate')).toBeTrue();
  });

  it('creates and edits only for the route camera', () => {
    component.allItems = [];
    component.startCreate();
    component.form.setValue({ date: new Date(2026, 7, 5), description: 'Cierre operativo' });
    component.save();
    expect(service.create).toHaveBeenCalledWith(jasmine.objectContaining({
      clearingHouseId: 7, date: '2026-08-05', description: 'Cierre operativo'
    }));

    component.startEdit(specialDate());
    component.form.patchValue({ description: 'Descripción actualizada' });
    component.save();
    expect(service.update).toHaveBeenCalledWith(jasmine.objectContaining({
      id: 1, clearingHouseId: 7, description: 'Descripción actualizada'
    }));
  });

  it('supports activation and inactivation without deleting records', () => {
    (component as any).changeStatus(specialDate());
    expect(service.changeStatus).toHaveBeenCalledWith(1, false);
    expect((service as any).delete).toBeUndefined();
  });

  it('renders Spanish text without mojibake markers', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Fechas especiales');
    expect(text).not.toContain('Ã');
    expect(text).not.toContain('â');
  });

  function specialDate(): any {
    return {
      id: 1, clearingHouseId: 7, clearingHouseName: 'ACH Colombia',
      date: `${new Date().getFullYear()}-08-05`, description: 'Cierre operativo', isActive: true
    };
  }
});

describe('ClearingHouseSpecialDatesLegacyRedirectComponent', () => {
  it('redirects a legacy route with camera id to the contextual route', async () => {
    await TestBed.configureTestingModule({
      imports: [ClearingHouseSpecialDatesLegacyRedirectComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { queryParamMap: of(convertToParamMap({ clearingHouseId: '7' })) } }
      ]
    }).compileComponents();
    const router = TestBed.inject(Router);
    const navigate = spyOn(router, 'navigate').and.resolveTo(true);
    const fixture = TestBed.createComponent(ClearingHouseSpecialDatesLegacyRedirectComponent);
    fixture.detectChanges();
    expect(navigate).toHaveBeenCalledWith(['/clearing-houses', 7, 'special-dates'], { replaceUrl: true });
  });
});
