import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigStateService } from '../services/nacha-config-state.service';
import { NachaConfigProfileWorkspacePageComponent } from './nacha-config-profile-workspace-page.component';

describe('NachaConfigProfileWorkspacePageComponent', () => {
  let fixture: ComponentFixture<NachaConfigProfileWorkspacePageComponent>;
  let component: NachaConfigProfileWorkspacePageComponent;
  let commandSpy: jasmine.SpyObj<NachaConfigCommandService>;

  beforeEach(async () => {
    spyOn(window, 'confirm').and.returnValue(true);
    commandSpy = jasmine.createSpyObj<NachaConfigCommandService>('NachaConfigCommandService', [
      'publicar',
      'validar',
      'editarBorrador',
      'clonar',
      'actualizarSecuencia',
      'actualizarVariante',
      'actualizarField',
      'actualizarRule',
      'inactivar',
      'archivar',
      'preview'
    ]);

    commandSpy.publicar.and.returnValue(of({ publicado: true } as any));
    commandSpy.validar.and.returnValue(of({ profileId: 1, isValid: false, erroresBloqueantes: 1, advertencias: 0, resumen: 'error', issues: [] }));
    commandSpy.editarBorrador.and.returnValue(of({} as any));
    commandSpy.clonar.and.returnValue(of({ id: 2 } as any));
    commandSpy.actualizarSecuencia.and.returnValue(of(void 0));
    commandSpy.actualizarVariante.and.returnValue(of(void 0));
    commandSpy.actualizarField.and.returnValue(of(void 0));
    commandSpy.actualizarRule.and.returnValue(of(void 0));
    commandSpy.inactivar.and.returnValue(of(void 0));
    commandSpy.archivar.and.returnValue(of(void 0));
    commandSpy.preview.and.returnValue(of({ success: true, layoutByRecordCode: {}, trace: [], warnings: [] } as any));

    await TestBed.configureTestingModule({
      imports: [SharedModule],
      declarations: [NachaConfigProfileWorkspacePageComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => '1' } } } },
        {
          provide: NachaConfigQueryService,
          useValue: {
            detalle: () => of({
              id: 1,
              profileCode: 'P1',
              nombreEs: 'Perfil',
              estado: 'BORRADOR',
              camara: 'ACH',
              flujo: 'ORIGINAL',
              direccion: 'SALIDA',
              versionMajor: 1,
              versionMinor: 0,
              effectiveFrom: '2026-01-01',
              effectiveTo: null,
              rowVersion: 'AAA=',
              contextPriority: 100,
              records: [],
              variantes: [{ id: 2, recordCode: '6', variantCode: 'VAR', nombreEs: 'V', priority: 1, isDefaultForRecord: true, totalLength: 106, fields: [{ id: 3, fieldCode: 'CAMPO', fieldNameEs: 'Campo', startPosition: 1, length: 10, propertyPath: 'x', sourceType: 'ENTIDAD', isEnabled: true, reglas: [{ id: 99, errorCode: 'R', errorMessageEs: 'Msg', severity: 'ERROR', isEnabled: true }] }] }]
            }),
            historial: () => of([]),
            snapshots: () => of([])
          }
        },
        { provide: NachaConfigCommandService, useValue: commandSpy },
        NachaConfigStateService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigProfileWorkspacePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('debe mostrar bloqueo de publicación cuando backend responde PUBLISH_BLOCKED', () => {
    commandSpy.publicar.and.returnValue(throwError(() => ({ errorCode: 'PUBLISH_BLOCKED', message: 'bloqueado', issues: [{ codigo: 'A', mensaje: 'B', severidad: 'ERROR' }] })));
    component.publicar();
    expect(component.alerta?.mensaje).toContain('bloqueada');
    expect(component.issuesPublicacion.length).toBe(1);
  });

  it('debe activar modo de concurrencia cuando hay conflicto', () => {
    commandSpy.editarBorrador.and.returnValue(throwError(() => ({ errorCode: 'CONCURRENCY_CONFLICT', message: 'conflicto', currentRowVersion: 'BBB=' })));
    component.guardarEdicion();
    expect(component.conflictosConcurrencia).toBeTrue();
  });

  it('debe cargar reglas y permitir seleccionar una', () => {
    const regla = component.rowsReglas[0];
    component.seleccionarRegla(regla);
    expect(component.reglaSeleccionada?.id).toBe(99);
    expect(component.ruleForm.controls.errorCode.value).toBe('R');
  });

  it('debe ejecutar vista previa', () => {
    component.ejecutarVistaPrevia();
    expect(commandSpy.preview).toHaveBeenCalled();
  });
});
