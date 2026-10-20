import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { AchClaimsApiService } from './ach-claims-api.service';
import { FinancialInstitutionsApiService } from '../../core/services/financial-institutions-api.service';
import { AchClaimsListComponent } from './ach-claims-list.component';
import { ClaimCaseStatus, ClaimCauseCode } from './ach-claims.models';

describe('AchClaimsListComponent', () => {
  let fixture: ComponentFixture<AchClaimsListComponent>;
  let component: AchClaimsListComponent;
  let api: jasmine.SpyObj<AchClaimsApiService>;
  let auth: jasmine.SpyObj<AuthService>;
  let institutionsApi: jasmine.SpyObj<FinancialInstitutionsApiService>;
  let allowCreate = true;

  const item = {
    id: 12,
    causeCode: ClaimCauseCode.Dev14,
    status: ClaimCaseStatus.Open,
    filedAt: '2026-09-30T14:00:00-05:00',
    deadlineAt: '2026-12-24T14:00:00-05:00',
    closedAt: null,
    externalReference: 'REF-12',
    requestingParticipant: { id: 2, name: 'Entidad Receptora', routingNumber: '002', transitCode: '02' },
    respondingParticipant: { id: 1, name: 'Entidad Originadora', routingNumber: '001', transitCode: '01' },
    transactionCount: 2
  };

  beforeEach(async () => {
    // Each test starts with its own authorized state, regardless of Jasmine's random test order.
    allowCreate = true;
    api = jasmine.createSpyObj('AchClaimsApiService', ['query']);
    api.query.and.returnValue(of({ items: [item], pageNumber: 1, pageSize: 25, totalItems: 51 }));
    institutionsApi = jasmine.createSpyObj('FinancialInstitutionsApiService', ['getAll']);
    institutionsApi.getAll.and.returnValue(of([]));
    auth = jasmine.createSpyObj('AuthService', ['hasPermission']);
    auth.hasPermission.and.callFake((permissions: string | string[]) => {
      const values = Array.isArray(permissions) ? permissions : [permissions];
      return allowCreate && values.includes('Claims.CreateDev14');
    });

    await TestBed.configureTestingModule({
      imports: [AchClaimsListComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        { provide: AchClaimsApiService, useValue: api },
        { provide: FinancialInstitutionsApiService, useValue: institutionsApi },
        { provide: AuthService, useValue: auth }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AchClaimsListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga reclamaciones con paginación server-side', () => {
    expect(api.query).toHaveBeenCalledWith(jasmine.objectContaining({ pageNumber: 1, pageSize: 25 }));
    component.pageChanged({ pageIndex: 1, pageSize: 50, length: 51 });
    expect(api.query.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ pageNumber: 2, pageSize: 50 }));
  });

  it('envía los filtros seleccionados al backend', () => {
    component.form.patchValue({ status: ClaimCaseStatus.Resolved, causeCode: ClaimCauseCode.Dev14, transactionId: 99, participantId: 8 });
    component.applyFilters();
    expect(api.query.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({
      status: ClaimCaseStatus.Resolved,
      causeCode: ClaimCauseCode.Dev14,
      transactionId: 99,
      participantId: 8,
      pageNumber: 1
    }));
  });

  it('presenta etiquetas humanizadas y no renderiza el enum técnico Open', () => {
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Reclamaciones');
    expect(text).toContain('Abierto');
    expect(text).toContain('Débito ACH no consentido');
    expect(text).not.toMatch(/\bOpen\b/);
    expect(text).not.toMatch(/\bClaimCase\b/);
  });

  it('navega al detalle después de crear una reclamación', () => {
    const dialog = (component as any).dialog as MatDialog;
    const router = (component as any).router as Router;
    spyOn(dialog, 'open').and.returnValue({ afterClosed: () => of({ id: 44 }) } as never);
    spyOn(router, 'navigate').and.resolveTo(true);

    component.openCreateDialog();

    expect(router.navigate).toHaveBeenCalledWith(['/ach-claims', 44]);
  });

  it('muestra el botón de nueva reclamación con permiso de creación', () => {
    expect(fixture.nativeElement.textContent).toContain('Nueva reclamación');
  });

  it('oculta el botón de nueva reclamación sin permiso de creación o gestión', async () => {
    allowCreate = false;
    fixture.destroy();
    fixture = TestBed.createComponent(AchClaimsListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Nueva reclamación');
  });
});
