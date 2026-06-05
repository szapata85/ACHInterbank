import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import {
  NachaConfigLayoutFieldEditRequest,
  NachaConfigLayoutVariantEditRequest,
  NachaConfigFieldRuleEditRequest,
  NachaConfigProfileDetail,
  NachaConfigProfileReadModel
} from '../models/nacha-config-admin.models';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigVariantsFieldsPageComponent } from './nacha-config-variants-fields-page.component';

describe('NachaConfigVariantsFieldsPageComponent', () => {
  let fixture: ComponentFixture<NachaConfigVariantsFieldsPageComponent>;
  let component: NachaConfigVariantsFieldsPageComponent;
  let querySpy: jasmine.SpyObj<NachaConfigQueryService>;
  let commandSpy: jasmine.SpyObj<NachaConfigCommandService>;
  let notificationsSpy: jasmine.SpyObj<NotificationService>;
  let router: Router;
  let authStub: { hasPermission: jasmine.Spy };

  const profiles: NachaConfigProfileReadModel[] = [
    {
      profileId: 11,
      profileCode: 'UAT-NACHA-CONFIG-001',
      profileName: 'Perfil borrador',
      clearingHouseCode: 'ACH',
      flowType: 'ORIGINAL',
      status: 'BORRADOR',
      version: '1.0',
      isPublished: false,
      isCurrent: true,
      effectiveFrom: '2026-01-01',
      effectiveTo: null,
      layoutVariantCount: 3,
      fieldCount: 4,
      recordTypes: ['1', '5'],
      isOfficialModel: true,
      legacyDeprecated: false
    }
  ];

  const detail: NachaConfigProfileDetail = {
    id: 11,
    profileCode: 'UAT-NACHA-CONFIG-001',
    nombreEs: 'Perfil borrador',
    descripcion: 'Perfil editable',
    estado: 'BORRADOR',
    camara: 'ACH',
    flujo: 'ORIGINAL',
    direccion: 'SALIDA',
    servicio: 'PPD',
    versionMajor: 1,
    versionMinor: 0,
    contextPriority: 100,
    effectiveFrom: '2026-01-01',
    effectiveTo: null,
    rowVersion: 'cm93',
    records: [
      { id: 101, recordCode: '1', sequence: 10, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'TABLE_DRIVEN' },
      { id: 102, recordCode: '5', sequence: 20, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'TABLE_DRIVEN' }
    ],
    variantes: [
      {
        id: 201,
        recordCode: '1',
        variantCode: 'R1_BASE',
        nombreEs: 'Record 1 base',
        priority: 1,
        isDefaultForRecord: true,
        totalLength: 106,
        fields: [
          {
            id: 301,
            fieldCode: 'FIELD_A',
            fieldNameEs: 'Field A',
            startPosition: 1,
            length: 10,
            propertyPath: 'Transaction.Amount',
            sourceType: 'CONSTANTE',
            isEnabled: true,
            reglas: [
              {
                id: 401,
                errorCode: 'ERR_REQUIRED',
                errorMessageEs: 'Campo requerido',
                severity: 'ERROR',
                isEnabled: true
              }
            ]
          },
          {
            id: 302,
            fieldCode: 'FIELD_B',
            fieldNameEs: 'Field B',
            startPosition: 11,
            length: 5,
            propertyPath: 'Transaction.Reference',
            sourceType: 'CONSTANTE',
            isEnabled: true,
            reglas: []
          }
        ]
      },
      {
        id: 202,
        recordCode: '1',
        variantCode: 'R1_ALT',
        nombreEs: 'Record 1 alternativa',
        priority: 2,
        isDefaultForRecord: false,
        totalLength: 106,
        fields: [
          {
            id: 303,
            fieldCode: 'FIELD_C',
            fieldNameEs: 'Field C',
            startPosition: 1,
            length: 8,
            propertyPath: 'Transaction.Code',
            sourceType: 'CONSTANTE',
            isEnabled: true,
            reglas: []
          }
        ]
      },
      {
        id: 203,
        recordCode: '5',
        variantCode: 'R5_BASE',
        nombreEs: 'Record 5 base',
        priority: 1,
        isDefaultForRecord: true,
        totalLength: 106,
        fields: [
          {
            id: 304,
            fieldCode: 'FIELD_D',
            fieldNameEs: 'Field D',
            startPosition: 1,
            length: 12,
            propertyPath: 'Company.Name',
            sourceType: 'CONSTANTE',
            isEnabled: true,
            reglas: []
          }
        ]
      }
    ]
  };

  const detailAfterVariantSave: NachaConfigProfileDetail = {
    ...detail,
    rowVersion: 'cm93LTI=',
    variantes: detail.variantes.map((variant) => variant.id === 201
      ? { ...variant, nombreEs: 'Record 1 base editado', priority: 7, isDefaultForRecord: false }
      : variant)
  };

  const detailAfterFieldSave: NachaConfigProfileDetail = {
    ...detail,
    rowVersion: 'cm93LTM=',
    variantes: detail.variantes.map((variant) => variant.id === 201
      ? { ...variant, fields: variant.fields.map((field) => field.id === 301 ? { ...field, fieldNameEs: 'Field A editado', startPosition: 3, length: 11, propertyPath: 'Transaction.Amount.Editado', isEnabled: false } : field) }
      : variant)
  };

  const detailAfterRuleSave: NachaConfigProfileDetail = {
    ...detail,
    rowVersion: 'cm93LTQ=',
    variantes: detail.variantes.map((variant) => variant.id === 201
      ? {
          ...variant,
          fields: variant.fields.map((field) => field.id === 301
            ? {
                ...field,
                reglas: field.reglas.map((rule) => rule.id === 401
                  ? { ...rule, errorCode: 'ERR_UPDATED', errorMessageEs: 'Mensaje actualizado', severity: 'WARN', isEnabled: false }
                  : rule)
              }
            : field)
        }
      : variant)
  };

  beforeEach(async () => {
    querySpy = jasmine.createSpyObj<NachaConfigQueryService>('NachaConfigQueryService', ['perfilesReadOnly', 'detalle']);
    commandSpy = jasmine.createSpyObj<NachaConfigCommandService>('NachaConfigCommandService', ['actualizarVariante', 'actualizarField', 'actualizarRule']);
    notificationsSpy = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error', 'warning', 'info']);
    authStub = { hasPermission: jasmine.createSpy().and.returnValue(true) };

    querySpy.perfilesReadOnly.and.returnValue(of(profiles));
    querySpy.detalle.and.returnValues(of(detail), of(detailAfterVariantSave), of(detailAfterFieldSave), of(detailAfterRuleSave));
    commandSpy.actualizarVariante.and.returnValue(of(void 0));
    commandSpy.actualizarField.and.returnValue(of(void 0));
    commandSpy.actualizarRule.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [SharedModule, RouterTestingModule, NachaConfigVariantsFieldsPageComponent],
      providers: [
        { provide: NachaConfigQueryService, useValue: querySpy },
        { provide: NachaConfigCommandService, useValue: commandSpy },
        { provide: NotificationService, useValue: notificationsSpy },
        { provide: AuthService, useValue: authStub }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigVariantsFieldsPageComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    fixture.detectChanges();
  });

  it('Component_ShouldLoadProfilesRecordsVariantsAndFields', () => {
    expect(querySpy.perfilesReadOnly).toHaveBeenCalled();
    expect(querySpy.detalle).toHaveBeenCalledWith(11);
    expect(component.selectedProfileId).toBe(11);
    expect(component.selectedRecordCode).toBe('1');
    expect(component.selectedVariantId).toBe(201);
    expect(component.selectedFieldId).toBe(301);
    expect(component.selectedRuleId).toBe(401);
    expect(component.recordVariants.map((variant) => variant.variantCode)).toEqual(['R1_BASE', 'R1_ALT']);
    expect(component.selectedFields.map((field) => field.fieldCode)).toEqual(['FIELD_A', 'FIELD_B']);
    expect(component.selectedRules.map((rule) => rule.errorCode)).toEqual(['ERR_REQUIRED']);
    expect(fixture.nativeElement.textContent).toContain('NACHA Config - Variants y Fields');
    expect(fixture.nativeElement.textContent).toContain('Record 1 base');
    expect(fixture.nativeElement.textContent).toContain('Field A');
    expect(fixture.nativeElement.textContent).toContain('Rules del field');
    expect(fixture.nativeElement.textContent).toContain('ERR_REQUIRED');
  });

  it('Component_ShouldAllowSelectingRecordVariantAndField', () => {
    component.onSelectRecord('5');
    expect(component.selectedRecordCode).toBe('5');
    expect(component.selectedVariantId).toBe(203);
    expect(component.selectedFieldId).toBe(304);

    component.onSelectRecord('1');
    component.onSelectVariant(202);
    expect(component.selectedVariantId).toBe(202);
    expect(component.selectedFieldId).toBe(303);

    component.onSelectField(302);
    expect(component.selectedFieldId).toBe(302);
    expect(component.selectedRuleId).toBeNull();

    component.onSelectVariant(201);
    component.onSelectField(301);
    expect(component.selectedVariantId).toBe(201);
    expect(component.selectedFieldId).toBe(301);
    expect(component.selectedRuleId).toBe(401);

    component.onSelectRule(401);
    expect(component.selectedRuleId).toBe(401);
  });

  it('Component_ShouldSaveVariantWithProfileRowVersionAndRefreshDetail', () => {
    const payload: NachaConfigLayoutVariantEditRequest = {
      nombreEs: 'Record 1 base editado',
      descripcion: 'Perfil editable',
      priority: 7,
      isDefaultForRecord: false,
      effectiveFrom: '2026-01-01',
      effectiveTo: null,
      expectedRowVersion: 'cm93'
    };

    component.variantForm.patchValue(payload);
    component.guardarVariant();

    expect(commandSpy.actualizarVariante).toHaveBeenCalledWith(11, 201, jasmine.objectContaining(payload as object));
    expect(querySpy.detalle).toHaveBeenCalledTimes(2);
    expect(component.selectedProfile?.rowVersion).toBe('cm93LTI=');
    expect(component.selectedVariant?.nombreEs).toBe('Record 1 base editado');
    expect(notificationsSpy.success).toHaveBeenCalledWith('Variant actualizada correctamente.');
  });

  it('Component_ShouldSaveFieldWithProfileRowVersionAndRefreshDetail', () => {
    querySpy.detalle.and.returnValue(of(detailAfterFieldSave));

    component.fieldForm.patchValue({
      fieldNameEs: 'Field A editado',
      startPosition: 3,
      length: 11,
      propertyPath: 'Transaction.Amount.Editado',
      isEnabled: false
    });
    component.guardarField();

    const expectedPayload: NachaConfigLayoutFieldEditRequest = {
      fieldNameEs: 'Field A editado',
      startPosition: 3,
      length: 11,
      propertyPath: 'Transaction.Amount.Editado',
      isEnabled: false,
      expectedRowVersion: 'cm93'
    };

    expect(commandSpy.actualizarField).toHaveBeenCalledWith(11, 301, jasmine.objectContaining(expectedPayload as object));
    expect(querySpy.detalle).toHaveBeenCalledTimes(2);
    expect(component.selectedProfile?.rowVersion).toBe('cm93LTM=');
    expect(component.selectedField?.fieldNameEs).toBe('Field A editado');
    expect(notificationsSpy.success).toHaveBeenCalledWith('Field actualizado correctamente.');
  });

  it('Component_ShouldSaveRuleWithProfileRowVersionAndRefreshDetail', () => {
    querySpy.detalle.and.returnValue(of(detailAfterRuleSave));

    component.onSelectRule(401);
    component.ruleForm.patchValue({
      errorCode: 'ERR_UPDATED',
      errorMessageEs: 'Mensaje actualizado',
      severity: 'WARN',
      isEnabled: false
    });
    component.guardarRule();

    const expectedPayload: NachaConfigFieldRuleEditRequest = {
      errorCode: 'ERR_UPDATED',
      errorMessageEs: 'Mensaje actualizado',
      severity: 'WARN',
      isEnabled: false,
      expectedRowVersion: 'cm93'
    };

    expect(commandSpy.actualizarRule).toHaveBeenCalledWith(11, 401, jasmine.objectContaining(expectedPayload as object));
    expect(querySpy.detalle).toHaveBeenCalledTimes(2);
    expect(component.selectedProfile?.rowVersion).toBe('cm93LTQ=');
    expect(component.selectedRule?.errorCode).toBe('ERR_UPDATED');
    expect(notificationsSpy.success).toHaveBeenCalledWith('Rule actualizada correctamente.');
  });

  it('Component_ShouldBlockEditingWhenReadonlyOrWithoutManagePermission', async () => {
    authStub.hasPermission.and.returnValue(false);
    querySpy.detalle.and.returnValue(of({
      ...detail,
      estado: 'PUBLICADO'
    }));

    fixture = TestBed.createComponent(NachaConfigVariantsFieldsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.puedeGestionar).toBeFalse();
    expect(component.puedeEditar).toBeFalse();
    expect(component.variantForm.disabled).toBeTrue();
    expect(component.fieldForm.disabled).toBeTrue();
    expect(component.ruleForm.disabled).toBeTrue();
    expect(fixture.nativeElement.textContent).not.toContain('Guardar variant');
    expect(fixture.nativeElement.textContent).not.toContain('Guardar field');
    expect(fixture.nativeElement.textContent).not.toContain('Guardar rule');
  });

  it('Component_ShouldSurfaceValidationAndConcurrencyErrors', () => {
    commandSpy.actualizarField.and.returnValue(throwError(() => ({
      message: 'El perfil fue modificado por otro usuario.',
      issues: [{ severidad: 'ERROR', codigo: 'CONCURRENCY_CONFLICT', mensaje: 'Concurrencia detectada.' }]
    })));

    component.fieldForm.patchValue({
      fieldNameEs: 'Field A editado',
      startPosition: 3,
      length: 11,
      propertyPath: 'Transaction.Amount.Editado',
      isEnabled: false
    });
    component.guardarField();

    expect(component.fieldSaveError).toContain('modificado por otro usuario');
    expect(component.fieldSaveIssues.length).toBe(1);
    expect(notificationsSpy.error).toHaveBeenCalled();
  });

  it('Component_ShouldBlockRuleEditingWhenReadonlyState', () => {
    querySpy.detalle.and.returnValue(of({
      ...detail,
      estado: 'PUBLICADO'
    }));

    fixture = TestBed.createComponent(NachaConfigVariantsFieldsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.puedeEditarRuleActual).toBeFalse();
    expect(component.ruleForm.disabled).toBeTrue();
    expect(fixture.nativeElement.textContent).not.toContain('Guardar rule');
  });

  it('Component_ShouldNavigateToProfileAndRecords', () => {
    component.irADetallePerfil();
    expect(router.navigate).toHaveBeenCalledWith(['/nacha-config-admin/perfiles', 11]);

    component.irARecords();
    expect(router.navigate).toHaveBeenCalledWith(['/nacha-config-admin/records']);
  });
});
