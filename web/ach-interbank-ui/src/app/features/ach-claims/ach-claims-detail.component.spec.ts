import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { AchClaimsApiService } from './ach-claims-api.service';
import { AchClaimsDetailComponent } from './ach-claims-detail.component';
import { AchTransactionType, ClaimCaseEventType, ClaimCaseStatus, ClaimCauseCode } from './ach-claims.models';

describe('AchClaimsDetailComponent', () => {
  let fixture: ComponentFixture<AchClaimsDetailComponent>;
  let api: jasmine.SpyObj<AchClaimsApiService>;
  let auth: jasmine.SpyObj<AuthService>;

  const detail = {
    id: 7,
    causeCode: ClaimCauseCode.Dev14,
    status: ClaimCaseStatus.Open,
    filedAt: '2026-09-30T14:00:00-05:00',
    deadlineAt: '2026-12-24T14:00:00-05:00',
    closedAt: null,
    externalReference: 'EXT-7',
    requestingParticipant: { id: 2, name: 'Entidad Receptora', routingNumber: '002', transitCode: '02' },
    respondingParticipant: { id: 1, name: 'Entidad Originadora', routingNumber: '001', transitCode: '01' },
    transactions: [{
      id: 100,
      traceNumber: 'TR-100',
      transactionExternalId: 'TX-100',
      amount: 125000,
      type: AchTransactionType.Debit,
      sourceInstitution: { id: 1, name: 'Entidad Originadora', routingNumber: '001', transitCode: '01' },
      destinationInstitution: { id: 2, name: 'Entidad Receptora', routingNumber: '002', transitCode: '02' }
    }],
    history: [
      { id: 1, eventType: ClaimCaseEventType.Opened, occurredAt: '2026-09-30T14:00:00-05:00', actor: 'operador', observation: 'Reclamación radicada.' },
      { id: 2, eventType: ClaimCaseEventType.Progress, occurredAt: '2026-10-01T14:00:00-05:00', actor: 'operador', observation: 'Gestión en curso.' }
    ],
    resolutionNotes: null
  };

  beforeEach(async () => {
    api = jasmine.createSpyObj('AchClaimsApiService', ['getById']);
    api.getById.and.returnValue(of(detail));
    auth = jasmine.createSpyObj('AuthService', ['hasPermission']);
    auth.hasPermission.and.returnValue(true);
    const paramMap = convertToParamMap({ id: '7' });

    await TestBed.configureTestingModule({
      imports: [AchClaimsDetailComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        { provide: AchClaimsApiService, useValue: api },
        { provide: AuthService, useValue: auth },
        { provide: ActivatedRoute, useValue: { paramMap: of(paramMap), snapshot: { paramMap } } }
      ]
    }).compileComponents();
    fixture = TestBed.createComponent(AchClaimsDetailComponent);
    fixture.detectChanges();
  });

  it('muestra datos principales, transacciones e historial con lenguaje operativo', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Reclamación #7');
    expect(text).toContain('Entidad solicitante');
    expect(text).toContain('Entidad destinataria');
    expect(text).toContain('TR-100');
    expect(text).toContain('Avance');
    expect(text).toContain('Débito');
    expect(text).toContain('Abierto');
    expect(text).not.toMatch(/\bOpen\b|\bProgress\b|\bClaimCase\b/);
  });

  it('muestra las acciones permitidas mientras la reclamación está abierta', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Registrar avance');
    expect(text).toContain('Resolver reclamación');
    expect(text).toContain('Cerrar por vencimiento');
  });

  it('oculta acciones cuando el usuario no tiene permisos de gestión', () => {
    auth.hasPermission.and.returnValue(false);
    fixture.destroy();
    fixture = TestBed.createComponent(AchClaimsDetailComponent);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).not.toContain('Registrar avance');
    expect(text).not.toContain('Resolver reclamación');
    expect(text).not.toContain('Cerrar por vencimiento');
  });

  it('oculta acciones incompatibles cuando la reclamación está resuelta', () => {
    fixture.componentInstance.detail.set({ ...detail, status: ClaimCaseStatus.Resolved, closedAt: '2026-10-02T10:00:00-05:00', resolutionNotes: 'Fondos gestionados.' });
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Resuelto');
    expect(text).toContain('Resultado de la reclamación');
    expect(text).not.toContain('Registrar avance');
    expect(text).not.toContain('Cerrar por vencimiento');
  });
});
