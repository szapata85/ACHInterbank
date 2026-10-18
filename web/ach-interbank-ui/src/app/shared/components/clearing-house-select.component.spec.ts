import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ClearingHousesApiService } from '../../core/services/clearing-houses-api.service';
import { ClearingHouseSelectComponent } from './clearing-house-select.component';

describe('ClearingHouseSelectComponent', () => {
  let fixture: ComponentFixture<ClearingHouseSelectComponent>;
  let component: ClearingHouseSelectComponent;
  let api: jasmine.SpyObj<ClearingHousesApiService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<ClearingHousesApiService>('ClearingHousesApiService', ['list']);
    api.list.and.returnValue(of([
      { id: 1, name: 'CENIT', code: 'CENIT' },
      { id: 2, name: 'ACH Colombia', code: 'ACHCOL' }
    ]));

    await TestBed.configureTestingModule({
      imports: [ClearingHouseSelectComponent],
      providers: [{ provide: ClearingHousesApiService, useValue: api }]
    }).compileComponents();

    fixture = TestBed.createComponent(ClearingHouseSelectComponent);
    component = fixture.componentInstance;
  });

  it('loads options through the transversal core service and preserves selection behavior', () => {
    const changes: Array<number | null> = [];
    component.allowAll = true;
    component.registerOnChange((value) => changes.push(value));

    fixture.detectChanges();
    component.writeValue(2);
    fixture.detectChanges();

    const options = Array.from(
      fixture.nativeElement.querySelectorAll('option') as NodeListOf<HTMLOptionElement>
    ).map((option) => option.textContent?.trim());

    expect(api.list).toHaveBeenCalledTimes(1);
    expect(options).toContain('CENIT (CENIT)');
    expect(options).toContain('ACH Colombia (ACHCOL)');
    expect(options).toContain('Todas las cámaras');
    expect(component.selectionControl.value).toBe(2);

    component.selectionControl.setValue(1);
    expect(changes).toEqual([1]);
  });

  it('keeps a contextual safe error state when options cannot be loaded', () => {
    api.list.and.returnValue(throwError(() => new Error('technical backend detail')));

    fixture.detectChanges();

    expect(component.options).toEqual([]);
    expect(component.hasError).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('No fue posible cargar cámaras');
    expect(fixture.nativeElement.textContent).not.toContain('technical backend detail');
  });
});
