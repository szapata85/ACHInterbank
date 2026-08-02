import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ClearingHousesService } from '../../clearing-houses/clearing-houses.service';
import { IncomingNachaCommandCenterService } from '../services/incoming-nacha-command-center.service';
import { NachaOperationalDashboardComponent } from './nacha-operational-dashboard.component';

describe('NachaOperationalDashboardComponent', () => {
  let fixture: ComponentFixture<NachaOperationalDashboardComponent>;
  let api: jasmine.SpyObj<IncomingNachaCommandCenterService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<IncomingNachaCommandCenterService>('IncomingNachaCommandCenterService', ['getFiles', 'getSummary']);
    api.getFiles.and.returnValue(of(filesPage()));
    api.getSummary.and.returnValue(of(summary()));
    const clearingHouses = jasmine.createSpyObj<ClearingHousesService>('ClearingHousesService', ['list']);
    clearingHouses.list.and.returnValue(of({ items: [{
      id: 1, name: 'CENIT', code: 'CENIT', originCode: '00000000', isActive: true,
      timeZoneId: 'America/Bogota', holidayStrategy: 'Colombia', requiresNachaProfile: true,
      activeCycleCount: 1, isReady: true, missingRequirements: [], createdAt: '2026-08-01T00:00:00Z'
    }], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 }));

    await TestBed.configureTestingModule({
      imports: [NachaOperationalDashboardComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: IncomingNachaCommandCenterService, useValue: api },
        { provide: ClearingHousesService, useValue: clearingHouses }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NachaOperationalDashboardComponent);
    fixture.detectChanges();
  });

  it('muestra una vista operativa en español con estados técnico y funcional separados', () => {
    const content = text();
    expect(content).toContain('Seguimiento de archivos NACHA-M');
    expect(content).toContain('Carga completada');
    expect(content).toContain('Procesado');
    expect(content).toContain('Procesado correctamente');
    expect(content).not.toContain('Command Center');
    expect(content).not.toContain('Dispatch');
  });

  it('aplica formato monetario colombiano con dos decimales', () => {
    expect(text()).toContain('$ 1.250.000,25');
    expect(text()).toContain('$ 2.500.000,50');
  });

  it('construye filtros, reinicia la página y conserva el ordenamiento en la URL', () => {
    const router = TestBed.inject(Router);
    const navigate = spyOn(router, 'navigate').and.resolveTo(true);
    fixture.componentInstance.filtersForm.patchValue({ fileName: ' archivo.OUT ', resultCode: 'r16', hasIssues: true });
    fixture.componentInstance.sortBy = 'fileName';
    fixture.componentInstance.sortDescending = false;

    fixture.componentInstance.applyFilters();

    expect(navigate).toHaveBeenCalledWith([], jasmine.objectContaining({
      queryParams: jasmine.objectContaining({ page: 1, fileName: 'archivo.OUT', resultCode: 'R16', hasIssues: true, sortBy: 'fileName' })
    }));
  });

  it('limpia todos los filtros sin conservar datos sensibles en almacenamiento local', () => {
    const storedBefore = Array.from({ length: localStorage.length }, (_, index) => localStorage.key(index)).sort();
    fixture.componentInstance.filtersForm.patchValue({ fileName: 'privado', resultCode: 'R17', hasTechnicalErrors: true });
    fixture.componentInstance.clearFilters();
    const value = fixture.componentInstance.filtersForm.getRawValue();
    expect(value.fileName).toBe('');
    expect(value.resultCode).toBe('');
    expect(value.hasTechnicalErrors).toBeFalse();
    const storedAfter = Array.from({ length: localStorage.length }, (_, index) => localStorage.key(index)).sort();
    expect(storedAfter).toEqual(storedBefore);
  });

  it('presenta un error recuperable y conserva la pantalla cuando falla la API', () => {
    api.getFiles.and.returnValue(throwError(() => ({ status: 500 })));
    const errorFixture = TestBed.createComponent(NachaOperationalDashboardComponent);
    errorFixture.detectChanges();
    expect(errorFixture.nativeElement.textContent).toContain('No fue posible consultar los archivos recibidos');
    expect(errorFixture.nativeElement.textContent).toContain('Reintentar');
  });

  it('navega al archivo con una URL recuperable y conserva la URL de retorno', () => {
    const router = TestBed.inject(Router);
    const navigate = spyOn(router, 'navigate').and.resolveTo(true);
    fixture.componentInstance.openFile(filesPage().items[0]);
    expect(navigate).toHaveBeenCalledWith(['files', '11111111-1111-1111-1111-111111111111'], jasmine.objectContaining({
      queryParams: jasmine.objectContaining({ seccion: 'resumen', retorno: jasmine.any(String) })
    }));
  });

  function text(): string { return fixture.nativeElement.textContent; }
});

function filesPage() {
  return {
    items: [{
      id: '11111111-1111-1111-1111-111111111111', fileName: '0001283.001.20260731.1', correlationId: 'corr-1',
      ingestionStatus: 'Completado' as const, ingestionStatusText: 'Completado', stageCode: 'Persisted', stageText: 'Carga completada',
      cycleResolutionStatus: 'ResueltoConfirmado', parsingStatus: 'Exitoso', resolvedClearingHouseId: 1,
      clearingHouseName: 'CENIT', resolvedAchCycleId: 'CICLO-01', operationalDate: '2026-07-31', uploadedAtUtc: '2026-08-01T14:00:00Z',
      uploadedBy: 'operador', queueItems: 2, processingEvents: 4, totalBatches: 1, totalTransactions: 2,
      totalDebit: 1250000.25, totalCredit: 2500000.5, processingStatusText: 'Procesado', overallResultText: 'Procesado correctamente',
      scheduledAtUtc: '2026-08-01T14:02:00Z', hasTechnicalErrors: false, hasIssues: false
    }],
    page: 1, pageSize: 20, totalItems: 1
  };
}

function summary() {
  return {
    generatedAtUtc: '2026-08-01T15:00:00Z', windowHours: 168,
    pipelineHealth: { totalIngestions: 1, totalQueueItems: 2, backlogItems: 0, blockedItems: 0, retryPendingItems: 0, failedFinalItems: 0, confirmedItems: 2 }
  };
}
