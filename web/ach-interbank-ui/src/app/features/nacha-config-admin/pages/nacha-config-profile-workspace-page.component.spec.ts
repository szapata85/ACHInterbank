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

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SharedModule],
      declarations: [NachaConfigProfileWorkspacePageComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => '1' } } } },
        {
          provide: NachaConfigQueryService,
          useValue: {
            detalle: () => of({ id: 1, profileCode: 'P1', nombreEs: 'Perfil', estado: 'BORRADOR', camara: 'ACH', flujo: 'ORIGINAL', direccion: 'SALIDA', versionMajor: 1, versionMinor: 0, effectiveFrom: '2026-01-01', effectiveTo: null, rowVersion: 'AAA=', contextPriority: 100, records: [], variantes: [] }),
            historial: () => of([]),
            snapshots: () => of([])
          }
        },
        {
          provide: NachaConfigCommandService,
          useValue: {
            publicar: () => throwError(() => ({ errorCode: 'PUBLISH_BLOCKED', message: 'bloqueado' })),
            validar: () => of({ profileId: 1, isValid: false, erroresBloqueantes: 1, advertencias: 0, resumen: 'error', issues: [] }),
            editarBorrador: () => of({}),
            clonar: () => of({ id: 2 }),
            actualizarSecuencia: () => of(void 0),
            actualizarVariante: () => of(void 0),
            actualizarField: () => of(void 0),
            actualizarRule: () => of(void 0),
            inactivar: () => of(void 0),
            archivar: () => of(void 0),
            preview: () => of({ success: true, layoutByRecordCode: {}, trace: [], warnings: [] })
          }
        },
        NachaConfigStateService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigProfileWorkspacePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('debe mostrar alerta cuando la publicación está bloqueada', () => {
    component.publicar();
    expect(component.alerta?.mensaje).toContain('bloqueada');
  });
});
