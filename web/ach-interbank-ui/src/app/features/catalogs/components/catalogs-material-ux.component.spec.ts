import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { CatalogTypesApiService } from '../services/catalog-types-api.service';
import { CompanyEntryDescriptionsApiService } from '../services/company-entry-descriptions-api.service';
import { CatalogTypesAdminComponent } from './catalog-types-admin.component';
import { CompanyEntryDescriptionAdminComponent } from './company-entry-description-admin.component';

const notifications = () => jasmine.createSpyObj('NotificationService', ['success', 'error']);
const dialogMock = (result: boolean) => ({
  open: jasmine.createSpy('open').and.returnValue({ afterClosed: () => of(result) })
});

describe('Catálogos — experiencia Material y lenguaje claro', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('aplica el editor Material al catálogo compartido de tipos y valida los campos', async () => {
    const api = jasmine.createSpyObj<CatalogTypesApiService>('CatalogTypesApiService', ['list', 'create', 'update', 'delete']);
    api.list.and.returnValue(of([]));
    const dialog = dialogMock(false);
    await TestBed.configureTestingModule({
      imports: [CatalogTypesAdminComponent],
      providers: [
        { provide: CatalogTypesApiService, useValue: api },
        { provide: NotificationService, useValue: notifications() },
        { provide: MatDialog, useValue: dialog },
        { provide: ActivatedRoute, useValue: { snapshot: { data: { catalogType: 'document-types', title: 'Tipos de documento' } } } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CatalogTypesAdminComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    expect(api.list).toHaveBeenCalledWith('document-types');
    expect(fixture.nativeElement.querySelectorAll('mat-form-field').length).toBe(3);
    expect(fixture.nativeElement.querySelector('button[mat-flat-button]')).not.toBeNull();
    component.form.patchValue({ code: '  ', name: 'Nombre' });
    expect(component.form.invalid).toBeTrue();
    component.form.patchValue({ code: 'DOC', name: 'Documento' });
    expect(component.form.valid).toBeTrue();
    component.edit({ code: 'DOC', name: 'Documento', description: null });
    expect(component.form.controls.code.disabled).toBeTrue();
    component.cancelEdit();
    expect(component.form.controls.code.enabled).toBeTrue();
    component.remove({ code: 'DOC', name: 'Documento', description: null });
    expect(dialog.open).toHaveBeenCalled();
    expect(api.delete).not.toHaveBeenCalled();
  });

  it('muestra errores recuperables al fallar la consulta del catálogo', async () => {
    const api = jasmine.createSpyObj<CatalogTypesApiService>('CatalogTypesApiService', ['list', 'create', 'update', 'delete']);
    api.list.and.returnValue(throwError(() => new Error('network')));
    await TestBed.configureTestingModule({
      imports: [CatalogTypesAdminComponent],
      providers: [
        { provide: CatalogTypesApiService, useValue: api },
        { provide: NotificationService, useValue: notifications() },
        { provide: MatDialog, useValue: dialogMock(false) },
        { provide: ActivatedRoute, useValue: { snapshot: { data: { catalogType: 'transaction-codes' } } } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CatalogTypesAdminComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.loadError).toBeTrue();
    expect(fixture.componentInstance.loading).toBeFalse();
  });

  it('usa selectores Material y conserva la clase SEC en los conceptos de lote', async () => {
    const api = jasmine.createSpyObj<CompanyEntryDescriptionsApiService>('CompanyEntryDescriptionsApiService', ['list', 'create', 'update', 'delete']);
    api.list.and.returnValue(of([]));
    api.create.and.returnValue(of({} as any));
    await TestBed.configureTestingModule({
      imports: [CompanyEntryDescriptionAdminComponent],
      providers: [
        { provide: CompanyEntryDescriptionsApiService, useValue: api },
        { provide: NotificationService, useValue: notifications() },
        { provide: MatDialog, useValue: dialogMock(false) }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CompanyEntryDescriptionAdminComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('mat-select').length).toBe(2);
    fixture.componentInstance.form.patchValue({
      term: 'pago', description: 'Pago de proveedores', standardEntryClassCode: 'CCD', isActive: true
    });
    fixture.componentInstance.submit();
    expect(api.create).toHaveBeenCalledWith(jasmine.objectContaining({ term: 'PAGO', standardEntryClassCode: 'CCD' }));
  });
});
