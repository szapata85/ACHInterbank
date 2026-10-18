import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigHistoryPanelComponent } from './nacha-config-history-panel.component';

describe('NachaConfigHistoryPanelComponent UX5', () => {
  let fixture: ComponentFixture<NachaConfigHistoryPanelComponent>;
  let query: jasmine.SpyObj<NachaConfigQueryService>;

  beforeEach(async () => {
    query = jasmine.createSpyObj<NachaConfigQueryService>('NachaConfigQueryService', ['historial']);
    query.historial.and.returnValue(of([{
      id: 1,
      changedAtUtc: '2026-10-11T18:00:00Z',
      changedBy: 'operador.ach',
      changeType: 'FIELD_EDITOR_UPDATE',
      entityName: 'CfgLayoutField',
      actionNameEs: 'Campo actualizado',
      elementNameEs: 'Número de cuenta',
      version: '2.0',
      summaryEs: 'Campo actualizado en Número de cuenta: 2 cambios.',
      detailEs: ['Posición inicial: 13 → 14', 'Origen del valor: actualizado']
    }]));

    await TestBed.configureTestingModule({
      imports: [NachaConfigHistoryPanelComponent],
      providers: [{ provide: NachaConfigQueryService, useValue: query }]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigHistoryPanelComponent);
    fixture.componentRef.setInput('perfil', {
      id: 11, profileCode: 'P', nombreEs: 'Perfil', estado: 'BORRADOR', estadoNombre: 'Borrador', isEditable: true,
      camara: 'ACH', camaraNombre: 'ACH Colombia', flujo: 'ORIGINAL', flujoNombre: 'Original', direccion: 'SALIDA', direccionNombre: 'Salida',
      versionMajor: 2, versionMinor: 0, contextPriority: 100, effectiveFrom: '2026-10-11', rowVersion: 'cm93', records: [], variantes: []
    });
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
  });

  it('muestra usuario fecha acción versión y detalle humanizado', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Campo actualizado');
    expect(text).toContain('Número de cuenta');
    expect(text).toContain('Versión 2.0');
    expect(text).toContain('operador.ach');
    expect(text).toContain('Ver detalle');

    const expandButton = fixture.nativeElement.querySelector('button[aria-expanded="false"]') as HTMLButtonElement;
    expect(expandButton).not.toBeNull();
    expandButton.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Posición inicial: 13 → 14');
  });
});
