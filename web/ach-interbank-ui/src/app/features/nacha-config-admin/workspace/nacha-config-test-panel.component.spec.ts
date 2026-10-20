import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { NachaConfigOperationalTestResult } from '../../../core/models/nacha-config.model';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigTestPanelComponent } from './nacha-config-test-panel.component';

describe('NachaConfigTestPanelComponent UX4', () => {
  let fixture: ComponentFixture<NachaConfigTestPanelComponent>;
  let component: NachaConfigTestPanelComponent;
  let command: jasmine.SpyObj<NachaConfigCommandService>;

  beforeEach(async () => {
    command = jasmine.createSpyObj<NachaConfigCommandService>('NachaConfigCommandService', ['probar']);
    command.probar.and.returnValue(of(buildResult()));

    await TestBed.configureTestingModule({
      imports: [NachaConfigTestPanelComponent],
      providers: [{ provide: NachaConfigCommandService, useValue: command }]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigTestPanelComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('profileId', 11);
    fixture.componentRef.setInput('selectedRecordCode', '6');
    fixture.componentRef.setInput('selectedFieldId', 101);
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
  });

  it('prueba la configuración y muestra validación + vista previa proveniente del backend', () => {
    component.probar();
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(command.probar).toHaveBeenCalledOnceWith(11);
    expect(text).toContain('Apto para publicar');
    expect(text).toContain('Estructura válida');
    expect(text).toContain('106 posiciones');
    expect(text).toContain('Número de cuenta');
    expect(text).toContain('Dato del sistema → Número de cuenta');
    expect(text).toContain('1234567890');
    expect(text).toContain('AB1234567890');

    // Only the selected field exposes its detailed source; select the second
    // preview block and mirror the parent's @Input update without changing the result.
    const fieldSelection = jasmine.createSpy('fieldSelection');
    component.selectedFieldIdChange.subscribe(fieldSelection);
    component.seleccionarCampo(102);
    expect(fieldSelection).toHaveBeenCalledOnceWith(102);
    fixture.componentRef.setInput('selectedFieldId', 102);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Cálculo configurado → Dígito de control');
  });

  it('humaniza errores y advertencias sin exponer el detalle técnico fuera de Información avanzada', () => {
    const invalid = buildResult();
    invalid.canPublish = false;
    invalid.resultLabelEs = 'Requiere correcciones';
    invalid.erroresBloqueantes = 2;
    invalid.advertencias = 1;
    invalid.issues = [
      {
        severity: 'ERROR', code: 'FIELD_OVERLAP',
        messageEs: 'Hay campos que ocupan posiciones superpuestas en Detalle de transacción.',
        suggestedActionEs: 'Ajusta las posiciones y vuelve a probar.',
        recordCode: '6', recordNameEs: 'Detalle de transacción', fieldId: 101, fieldNameEs: 'Número de cuenta',
        technicalMessage: 'Field ACCOUNT overlaps field CHECK_DIGIT.'
      },
      {
        severity: 'WARN', code: 'MISSING_SOURCE',
        messageEs: 'Un campo obligatorio no tiene un origen de valor completo.',
        technicalMessage: 'source missing'
      }
    ];
    command.probar.and.returnValue(of(invalid));

    component.probar();
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Requiere correcciones');
    expect(text).toContain('Hay campos que ocupan posiciones superpuestas');
    expect(text).toContain('Un campo obligatorio no tiene un origen de valor completo');
    expect(text).toContain('Información avanzada');
  });

  it('sincroniza selección de campo entre la vista previa y el workspace sin mutar configuración', () => {
    const selected = jasmine.createSpy('selected');
    component.selectedFieldIdChange.subscribe(selected);
    component.probar();
    component.seleccionarCampo(102);

    expect(selected).toHaveBeenCalledWith(102);
    expect(command.probar).toHaveBeenCalledTimes(1);
  });

  it('presenta estado de error conservando la configuración sin cambios', () => {
    command.probar.and.returnValue(throwError(() => new Error('offline')));

    component.probar();
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();

    expect(component.errorMessage).toContain('conserva sin cambios');
    expect(fixture.nativeElement.textContent).toContain('La configuración se conserva sin cambios');
  });
});

function buildResult(): NachaConfigOperationalTestResult {
  return {
    profileId: 11,
    processDateUtc: '2026-10-11T00:00:00Z',
    canPublish: true,
    resultLabelEs: 'Apto para publicar',
    erroresBloqueantes: 0,
    advertencias: 0,
    issues: [],
    selection: {
      success: true,
      summaryEs: 'Para ACH Colombia / Original / Salida, el backend resolverá esta configuración.',
      resolvedProfileId: 11,
      resolvedProfileCode: 'ACH_V1',
      warningsEs: [],
      technicalTrace: []
    },
    records: [{
      variantId: 20,
      recordCode: '6',
      recordNameEs: 'Detalle de transacción',
      variantCode: 'ACH_R6',
      variantNameEs: 'Detalle base',
      totalLength: 106,
      isDefaultForRecord: true,
      contentPreviewAvailable: true,
      renderedRecord: 'AB1234567890'.padEnd(106, ' '),
      fields: [
        {
          fieldId: 101,
          fieldNameEs: 'Número de cuenta',
          startPosition: 3,
          endPosition: 12,
          length: 10,
          isEnabled: true,
          isRequired: true,
          sourceSummaryEs: 'Dato del sistema → Número de cuenta',
          formatSummaryEs: 'Sin formato especial',
          sampleValue: '1234567890',
          relevantRulesEs: ['Contenido: valor requerido']
        },
        {
          fieldId: 102,
          fieldNameEs: 'Dígito de control',
          startPosition: 13,
          endPosition: 13,
          length: 1,
          isEnabled: true,
          isRequired: false,
          sourceSummaryEs: 'Cálculo configurado → Dígito de control',
          sampleValue: '7',
          relevantRulesEs: []
        }
      ]
    }]
  };
}
