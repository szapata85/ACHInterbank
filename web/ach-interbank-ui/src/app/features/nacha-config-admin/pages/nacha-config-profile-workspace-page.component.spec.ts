import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { NachaConfigProfileWorkspacePageComponent } from './nacha-config-profile-workspace-page.component';

describe('NachaConfigProfileWorkspacePageComponent', () => {
  let fixture: ComponentFixture<NachaConfigProfileWorkspacePageComponent>;
  let component: NachaConfigProfileWorkspacePageComponent;
  let commandSpy: jasmine.SpyObj<NachaConfigCommandService>;

  beforeEach(async () => {
    commandSpy = jasmine.createSpyObj<NachaConfigCommandService>('NachaConfigCommandService', ['editarBorrador', 'publicar', 'actualizarField']);

    await TestBed.configureTestingModule({
      imports: [SharedModule, RouterTestingModule],
      declarations: [NachaConfigProfileWorkspacePageComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => '1' } } } },
        {
          provide: NachaConfigQueryService,
          useValue: {
            detalleReadOnly: () => of({
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
              layoutVariantCount: 1,
              fieldCount: 1,
              recordTypes: ['1', '5', '6', '7', '8', '9'],
              isOfficialModel: true,
              legacyDeprecated: true,
              variants: [{ variantId: 1, variantCode: 'ACH_R6_BASE_V1', recordType: '6', recordLength: 106, blockingFactor: 10, isActive: true, fieldCount: 1 }],
              fields: [{ fieldId: 2, recordType: '6', fieldName: 'Amount', startPosition: 30, length: 10, endPosition: 39, dataType: 'ENTIDAD', isRequired: true, defaultValue: null, sourceFieldPath: 'AchTransaction.Amount', paddingDirection: 'LeftPad', paddingChar: '0', format: null, isComputed: false, isControlTotalField: false }]
            })
          }
        },
        { provide: NachaConfigCommandService, useValue: commandSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaConfigProfileWorkspacePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('Component_ShouldCreate', () => {
    expect(component).toBeTruthy();
  });

  it('Component_ShouldRenderVariantsAndFields', () => {
    expect(component.perfil?.variants.length).toBe(1);
    expect(component.perfil?.fields.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('sourceFieldPath');
  });

  it('Component_ShouldNotRenderCreateEditPublishDeleteButtons', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).not.toContain('Guardar');
    expect(text).not.toContain('Publicar');
    expect(text).not.toContain('Archivar');
    expect(text).not.toContain('Eliminar');
  });

  it('Component_ShouldNotInvokeNachaConfigCommandService', () => {
    expect(commandSpy.editarBorrador).not.toHaveBeenCalled();
    expect(commandSpy.publicar).not.toHaveBeenCalled();
    expect(commandSpy.actualizarField).not.toHaveBeenCalled();
  });
});
