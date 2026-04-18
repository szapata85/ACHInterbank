import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigProfilesPageComponent } from './nacha-config-profiles-page.component';

describe('NachaConfigProfilesPageComponent', () => {
  let fixture: ComponentFixture<NachaConfigProfilesPageComponent>;
  let component: NachaConfigProfilesPageComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SharedModule, RouterTestingModule],
      declarations: [NachaConfigProfilesPageComponent],
      providers: [
        {
          provide: NachaConfigQueryService,
          useValue: {
            perfiles: () => of([{ id: 1, profileCode: 'P1', nombreEs: 'Perfil', estado: 'BORRADOR', camara: 'ACH', flujo: 'ORIGINAL', direccion: 'SALIDA', versionMajor: 1, versionMinor: 0, effectiveFrom: '2026-01-01', effectiveTo: null, rowVersion: 'AAA=' }]),
            catalogosFiltro: () => of({
              estados: [{ code: 'BORRADOR', labelEs: 'BORRADOR' }],
              camaras: [{ code: 'ACH', labelEs: 'ACH Colombia' }],
              flujos: [{ code: 'ORIGINAL', labelEs: 'Original' }],
              direcciones: [{ code: 'SALIDA', labelEs: 'Salida' }],
              servicios: [{ code: 'PPD', labelEs: 'PPD' }]
            })
          }
        },
        {
          provide: NachaConfigCommandService,
          useValue: {
            crearBorrador: () => of({ id: 1 })
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigProfilesPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('debe renderizar listado de perfiles', () => {
    expect(component.visibles.length).toBe(1);
    expect(component.visibles[0].profileCode).toBe('P1');
  });
});
