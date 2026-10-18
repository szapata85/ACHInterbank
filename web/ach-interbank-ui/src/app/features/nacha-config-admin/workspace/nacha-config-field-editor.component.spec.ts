import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { MatSelectHarness } from '@angular/material/select/testing';
import { of, throwError } from 'rxjs';
import { NachaConfigFieldEditor } from '../../../core/models/nacha-config.model';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigFieldEditorComponent } from './nacha-config-field-editor.component';

describe('NachaConfigFieldEditorComponent UX3', () => {
  let fixture: ComponentFixture<NachaConfigFieldEditorComponent>;
  let component: NachaConfigFieldEditorComponent;
  let query: jasmine.SpyObj<NachaConfigQueryService>;
  let command: jasmine.SpyObj<NachaConfigCommandService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let editor: NachaConfigFieldEditor;

  beforeEach(async () => {
    editor = buildEditor(true);
    query = jasmine.createSpyObj('NachaConfigQueryService', ['editorCampo']);
    command = jasmine.createSpyObj('NachaConfigCommandService', ['actualizarEditorCampo']);
    notifications = jasmine.createSpyObj('NotificationService', ['success', 'error']);
    query.editorCampo.and.returnValue(of(editor));
    command.actualizarEditorCampo.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [NachaConfigFieldEditorComponent],
      providers: [
        { provide: NachaConfigQueryService, useValue: query },
        { provide: NachaConfigCommandService, useValue: command },
        { provide: NotificationService, useValue: notifications },
        { provide: AuthService, useValue: { hasPermission: () => true } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigFieldEditorComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('profileId', 11);
    fixture.componentRef.setInput('fieldId', 101);
    fixture.componentRef.setInput('profileEditable', true);
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
  });

  it('loads humanized editor options without exposing PropertyPath in the normal source selector', async () => {
    const loader = TestbedHarnessEnvironment.loader(fixture);
    const sourceType = await loader.getHarness(MatSelectHarness.with({ selector: 'mat-select[formControlName="sourceTypeCode"]' }));
    await sourceType.open();
    const sourceTypeLabels = await Promise.all((await sourceType.getOptions()).map(option => option.getText()));
    expect(sourceTypeLabels).toContain('Dato del sistema');
    await sourceType.close();
    const sourceData = await loader.getHarness(MatSelectHarness.with({ selector: 'mat-select[formControlName="sourceDefinitionId"]' }));
    await sourceData.open();
    const sourceDataLabels = await Promise.all((await sourceData.getOptions()).map(option => option.getText()));
    expect(sourceDataLabels.join(' ')).toContain('Número de cuenta del beneficiario');
    expect(sourceDataLabels.join(' ')).not.toContain('Beneficiary.Account.Number');
    expect(query.editorCampo).toHaveBeenCalledWith(11, 101);
    expect(fixture.nativeElement.querySelectorAll('.editor-section')[1].textContent).toContain('Origen del valor');
  });

  it('saves fixed value, format, alignment, padding and rules through the typed UX3 command', () => {
    component.form.patchValue({
      fieldNameEs: 'Número de cuenta destino',
      startPosition: 12,
      length: 18,
      padChar: '0',
      justification: 'R',
      formatMask: 'ACCOUNT_FMT',
      sourceTypeCode: 'CONSTANTE',
      sourceDefinitionId: null,
      fixedValue: '123'
    });
    component.sourceTypeChanged();
    component.rules.at(0).patchValue({ errorMessageEs: 'Cuenta obligatoria', severity: 'WARN', isEnabled: true });

    component.save();

    expect(command.actualizarEditorCampo).toHaveBeenCalledWith(11, 101, jasmine.objectContaining({
      fieldNameEs: 'Número de cuenta destino',
      startPosition: 12,
      length: 18,
      padChar: '0',
      justification: 'R',
      formatMask: 'ACCOUNT_FMT',
      source: jasmine.objectContaining({ sourceTypeCode: 'CONSTANTE', fixedValue: '123' }),
      expectedRowVersion: 'cm93'
    }));
    const payload = command.actualizarEditorCampo.calls.mostRecent().args[2];
    expect(payload.rules[0]).toEqual(jasmine.objectContaining({ id: 701, severity: 'WARN', errorMessageEs: 'Cuenta obligatoria' }));
  });

  it('changes to a persisted calculation by opaque source id instead of an expression', () => {
    component.form.patchValue({ sourceTypeCode: 'EXPRESION', sourceDefinitionId: 902 });
    component.sourceTypeChanged();
    component.save();

    const payload = command.actualizarEditorCampo.calls.mostRecent().args[2];
    expect(payload.source.sourceDefinitionId).toBe(902);
    expect(JSON.stringify(payload)).not.toContain('expressionDsl');
  });

  it('protects a published configuration from direct save', () => {
    editor = buildEditor(false);
    query.editorCampo.and.returnValue(of(editor));
    component.profileEditable = false;
    component.ngOnChanges();
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();

    component.save();

    expect(command.actualizarEditorCampo).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('solo consulta');
  });

  it('shows the required concurrency message and does not hide the conflict', () => {
    command.actualizarEditorCampo.and.returnValue(throwError(() => ({ errorCode: 'CONCURRENCY_CONFLICT' })));

    component.save();
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();

    expect(component.saveError).toBe('La configuración fue modificada por otro usuario. Recarga los cambios antes de continuar.');
    expect(notifications.error).toHaveBeenCalledWith(component.saveError);
  });
});

function buildEditor(editable: boolean): NachaConfigFieldEditor {
  return {
    profileId: 11,
    fieldId: 101,
    isEditable: editable,
    rowVersion: 'cm93',
    sourceReferenceCount: 2,
    sourceTypes: [
      { code: 'CONSTANTE', labelEs: 'Valor fijo', catalogLabelEs: 'Valor constante', usesFixedValue: true, requiresDefinitionSelection: false },
      { code: 'ENTIDAD', labelEs: 'Dato del sistema', catalogLabelEs: 'Entidad del dominio', usesFixedValue: false, requiresDefinitionSelection: true },
      { code: 'EXPRESION', labelEs: 'Calculado por el sistema', catalogLabelEs: 'Expresión declarativa', usesFixedValue: false, requiresDefinitionSelection: true }
    ],
    sourceOptions: [
      { sourceDefinitionId: 901, sourceTypeCode: 'ENTIDAD', labelEs: 'Dato del sistema → Número de cuenta del beneficiario' },
      { sourceDefinitionId: 902, sourceTypeCode: 'EXPRESION', labelEs: 'Calculado por el sistema → Dígito de control' }
    ],
    alignments: [{ code: 'L', labelEs: 'Alinear a la izquierda' }, { code: 'R', labelEs: 'Alinear a la derecha' }],
    formats: [{ code: 'ACCOUNT_FMT', labelEs: 'Formato usado en Número de cuenta del beneficiario' }],
    severities: [{ code: 'ERROR', labelEs: 'Error bloqueante' }, { code: 'WARN', labelEs: 'Advertencia' }],
    field: {
      id: 101,
      fieldCode: 'ACCOUNT',
      fieldNameEs: 'Número de cuenta',
      startPosition: 5,
      length: 17,
      padChar: ' ',
      justification: 'L',
      formatMask: null,
      sourceDefinitionId: 901,
      sourceType: 'ENTIDAD',
      sourceTypeName: 'Entidad del dominio',
      propertyPath: 'Beneficiary.Account.Number',
      isEnabled: true,
      reglas: [{
        id: 701,
        ruleType: 'REQUIRED',
        ruleTypeName: 'Obligatorio',
        errorCode: 'REQ_ACCOUNT',
        errorMessageEs: 'La cuenta es obligatoria',
        severity: 'ERROR',
        severityName: 'Error bloqueante',
        ruleCode: 'ACCOUNT_REQUIRED',
        conditionDsl: null,
        ruleConfigJson: null,
        order: 1,
        isEnabled: true
      }]
    }
  };
}
