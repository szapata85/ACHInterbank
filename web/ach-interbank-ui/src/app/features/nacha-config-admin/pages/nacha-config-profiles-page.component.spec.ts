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
  let commandSpy: jasmine.SpyObj<NachaConfigCommandService>;

  beforeEach(async () => {
    commandSpy = jasmine.createSpyObj<NachaConfigCommandService>('NachaConfigCommandService', ['crearBorrador', 'publicar']);

    await TestBed.configureTestingModule({
      imports: [SharedModule, RouterTestingModule],
      declarations: [NachaConfigProfilesPageComponent],
      providers: [
        {
          provide: NachaConfigQueryService,
          useValue: {
            dashboardReadOnly: () => of({
              productiveStatus: 'NO-GO',
              isOfficialModel: true,
              legacyDeprecated: true,
              profileCount: 1,
              publishedProfileCount: 1,
              currentProfileCount: 1,
              layoutVariantCount: 6,
              fieldCount: 20,
              clearingHouses: ['ACH'],
              recordTypes: ['1', '5', '6', '7', '8', '9'],
              warnings: []
            }),
            perfilesReadOnly: () => of([{
              profileId: 1,
              profileCode: 'OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0',
              profileName: 'Perfil oficial',
              clearingHouseCode: 'ACH',
              flowType: 'ORIGINAL',
              status: 'PUBLICADO',
              version: 'v1.0',
              isPublished: true,
              isCurrent: true,
              effectiveFrom: '2026-01-01',
              effectiveTo: null,
              layoutVariantCount: 6,
              fieldCount: 20,
              recordTypes: ['1', '5', '6', '7', '8', '9'],
              isOfficialModel: true,
              legacyDeprecated: true
            }]),
            catalogosFiltro: () => of({
              estados: [{ code: 'PUBLICADO', labelEs: 'PUBLICADO' }],
              camaras: [{ code: 'ACH', labelEs: 'ACH Colombia' }],
              flujos: [{ code: 'ORIGINAL', labelEs: 'Original' }],
              direcciones: [{ code: 'SALIDA', labelEs: 'Salida' }],
              servicios: [{ code: 'PPD', labelEs: 'PPD' }]
            })
          }
        },
        { provide: NachaConfigCommandService, useValue: commandSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigProfilesPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('Component_ShouldCreate', () => {
    expect(component).toBeTruthy();
  });

  it('Component_ShouldRenderOfficialModelBanner', () => {
    expect(fixture.nativeElement.textContent).toContain('Modelo oficial NACHA-M');
  });

  it('Component_ShouldRenderNoGoBanner', () => {
    expect(fixture.nativeElement.textContent).toContain('Productivo NO-GO');
  });

  it('Component_ShouldRenderLegacyDeprecatedNotice', () => {
    expect(fixture.nativeElement.textContent).toContain('Legacy layouts/definitions deprecated');
  });

  it('Component_ShouldRenderProfilesTable', () => {
    expect(component.visibles.length).toBe(1);
    expect(component.visibles[0].profileCode).toContain('OFFICIAL_ACH');
  });

  it('Component_ShouldNotRenderCreateEditPublishDeleteButtons', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).not.toContain('Crear borrador');
    expect(text).not.toContain('Publicar');
    expect(text).not.toContain('Eliminar');
    expect(text).not.toContain('Guardar');
  });

  it('Component_ShouldNotInvokeNachaConfigCommandService', () => {
    expect(commandSpy.crearBorrador).not.toHaveBeenCalled();
    expect(commandSpy.publicar).not.toHaveBeenCalled();
  });
});
