import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpHeaders, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { TraceabilityReportComponent } from './traceability-report.component';
import { ReportsApiService } from '../services/reports-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AchCyclesApiService } from '../../ach-cycles/services/ach-cycles-api.service';

describe('TraceabilityReportComponent', () => {
  let fixture: ComponentFixture<TraceabilityReportComponent>;
  let component: TraceabilityReportComponent;
  let api: jasmine.SpyObj<ReportsApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  let cyclesApi: jasmine.SpyObj<AchCyclesApiService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<ReportsApiService>('ReportsApiService', ['downloadTraceabilityPdf']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);
    cyclesApi = jasmine.createSpyObj<AchCyclesApiService>('AchCyclesApiService', ['search']);
    cyclesApi.search.and.returnValue(of({
      items: [
        { id: 'cycle-1', cycleName: 'Ciclo 1', clearingHouseName: 'ACH Colombia' },
        { id: 'cycle-2', cycleName: 'Ciclo 2', clearingHouseName: 'CENIT' }
      ]
    } as any));

    await TestBed.configureTestingModule({
      imports: [TraceabilityReportComponent],
      providers: [
        { provide: ReportsApiService, useValue: api },
        { provide: NotificationService, useValue: notifications },
        { provide: AchCyclesApiService, useValue: cyclesApi },
        { provide: ActivatedRoute, useValue: {} },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TraceabilityReportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('TraceabilityReport_ShouldGenerateNonEmptyPdf_WhenDataExists', async () => {
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:traceability');
    spyOn(window.URL, 'revokeObjectURL');
    const originalCreateElement = document.createElement.bind(document);
    const anchor = originalCreateElement('a') as HTMLAnchorElement;
    spyOn(anchor, 'click');
    const createElementSpy = spyOn(document, 'createElement').and.callFake((tagName: string, options?: ElementCreationOptions) => {
      return tagName.toLowerCase() === 'a' ? anchor : originalCreateElement(tagName, options);
    });
    const pdfBytes = new Blob(['%PDF-1.4\n', 'x'.repeat(1024)], { type: 'application/pdf' });
    api.downloadTraceabilityPdf.and.returnValue(of(new HttpResponse({
      body: pdfBytes,
      headers: new HttpHeaders({ 'content-type': 'application/pdf' })
    })));

    component.generatePdf();
    await new Promise((resolve) => setTimeout(resolve, 500));
    createElementSpy.and.callThrough();

    expect(window.URL.createObjectURL).toHaveBeenCalled();
    expect(anchor.click).toHaveBeenCalled();
    expect(notifications.success).toHaveBeenCalledWith('El reporte de trazabilidad se descargó correctamente.');
  });

  it('TraceabilityReport_ShouldDeduplicateMultipleSelection', async () => {
    api.downloadTraceabilityPdf.and.returnValue(of(new HttpResponse({
      body: new Blob([], { type: 'application/pdf' }),
      headers: new HttpHeaders({ 'content-type': 'application/pdf' })
    })));
    component.form.patchValue({ achCycleId: ['cycle-1', 'cycle-1', 'cycle-2'] });

    component.generatePdf();
    await new Promise((resolve) => setTimeout(resolve));

    expect(api.downloadTraceabilityPdf).toHaveBeenCalledWith(jasmine.objectContaining({
      achCycleId: ['cycle-1', 'cycle-2']
    }));
  });

  it('ReportExport_ShouldNotShowSuccess_WhenPdfWasNotGenerated', async () => {
    api.downloadTraceabilityPdf.and.returnValue(of(new HttpResponse({
      body: new Blob([], { type: 'application/pdf' }),
      headers: new HttpHeaders({ 'content-type': 'application/pdf' })
    })));

    component.generatePdf();
    await new Promise((resolve) => setTimeout(resolve));
    fixture.detectChanges();

    expect(notifications.success).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('No encontramos información para incluir en el reporte.');
    const message = fixture.nativeElement.querySelector('[data-testid="traceability-export-error"]') as HTMLElement;
    expect(message?.textContent).toContain('No encontramos información para incluir en el reporte.');
  });

  it('TraceabilityReport_ShouldKeepEventsReadable', () => {
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    const subtitle = fixture.nativeElement.querySelector('mat-card-subtitle') as HTMLElement;

    expect(button.textContent).toContain('Descargar PDF');
    expect(subtitle.textContent).toContain('hora de Colombia');
  });
});
