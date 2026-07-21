import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ClearingHousesComponent } from './clearing-houses.component';
import { ClearingHousesService } from './clearing-houses.service';

describe('ClearingHousesComponent', () => {
  let fixture: ComponentFixture<ClearingHousesComponent>;
  let api: jasmine.SpyObj<ClearingHousesService>;
  const row = { id: 3, code: 'NUEVARED', name: 'Nueva Red', originCode: '900', isActive: false,
    timeZoneId: 'America/Bogota', holidayStrategy: 'Colombian', requiresNachaProfile: false,
    activeCycleCount: 0, isReady: false, missingRequirements: ['Al menos un ciclo activo y vigente'], createdAt: new Date().toISOString() };

  beforeEach(async () => {
    api = jasmine.createSpyObj('ClearingHousesService', ['list', 'get', 'create', 'update', 'changeStatus', 'profiles']);
    api.list.and.returnValue(of({ items: [row], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 }));
    api.profiles.and.returnValue(of([]));
    await TestBed.configureTestingModule({
      imports: [ClearingHousesComponent],
      providers: [provideRouter([]), { provide: ClearingHousesService, useValue: api },
        { provide: AuthService, useValue: { hasPermission: () => true } }]
    }).compileComponents();
    fixture = TestBed.createComponent(ClearingHousesComponent);
    fixture.detectChanges();
  });

  it('renderiza lista, estado y configuración incompleta en español', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Cámaras compensadoras'); expect(text).toContain('NUEVARED'); expect(text).toContain('Inactiva');
    fixture.componentInstance.view(row); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Configuración incompleta');
    expect(fixture.nativeElement.textContent).not.toContain('[object Object]');
  });

  it('busca y filtra activas desde el DOM', () => {
    fixture.componentInstance.search = 'nueva'; fixture.componentInstance.state = 'active'; fixture.componentInstance.applyFilters();
    expect(api.list).toHaveBeenCalledWith('nueva', true, 1);
  });

  it('normaliza y crea una cámara evitando doble envío', () => {
    const pending = new Subject<typeof row>(); api.create.and.returnValue(pending); fixture.componentInstance.create();
    fixture.componentInstance.form.setValue({ code: ' nuevared ', name: 'Nueva Red', originCode: '900', timeZoneId: 'America/Bogota', holidayStrategy: 'Colombian', requiresNachaProfile: false, nachaProfileId: null });
    fixture.componentInstance.save(); fixture.componentInstance.save();
    expect(api.create).toHaveBeenCalledTimes(1); expect(api.create.calls.mostRecent().args[0].code).toBe('NUEVARED');
  });

  it('muestra conflicto funcional sin object Object', () => {
    api.create.and.returnValue(throwError(() => ({ error: { detail: 'Ya existe una cámara con ese código.' } })));
    fixture.componentInstance.create(); fixture.componentInstance.form.setValue({ code: 'NUEVARED', name: 'Nueva Red', originCode: '900', timeZoneId: 'America/Bogota', holidayStrategy: 'Colombian', requiresNachaProfile: false, nachaProfileId: null });
    fixture.componentInstance.save(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Ya existe una cámara'); expect(fixture.nativeElement.textContent).not.toContain('[object Object]');
  });

  it('reactiva y conserva las acciones de ciclos', () => {
    api.changeStatus.and.returnValue(of({ ...row, isActive: true, isReady: true, activeCycleCount: 1, missingRequirements: [] }));
    fixture.componentInstance.changeStatus(row); fixture.detectChanges();
    expect(api.changeStatus).toHaveBeenCalledWith(3, true);
    expect(fixture.nativeElement.textContent).toContain('Administrar ciclos');
  });
});
