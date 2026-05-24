import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpHeaders, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';
import { ReconciliationReportComponent } from './reconciliation-report.component';
import { ReportsApiService } from '../services/reports-api.service';
import { NotificationService } from '../../../core/services/notification.service';

describe('ReconciliationReportComponent', () => {
  let fixture: ComponentFixture<ReconciliationReportComponent>;
  let component: ReconciliationReportComponent;
  let api: jasmine.SpyObj<ReportsApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  const emptyData = {
    totals: {
      sentCount: 0,
      sentAmount: 0,
      receivedCount: 0,
      receivedAmount: 0,
      returnedCount: 0,
      returnedAmount: 0
    },
    differences: {
      sentVsReceivedCountDiff: 0,
      sentVsReceivedAmountDiff: 0,
      sentVsReturnedCountDiff: 0,
      sentVsReturnedAmountDiff: 0,
      receivedVsReturnedCountDiff: 0,
      receivedVsReturnedAmountDiff: 0
    },
    inconsistencies: []
  };

  beforeEach(async () => {
    api = jasmine.createSpyObj<ReportsApiService>('ReportsApiService', [
      'getReconciliation',
      'downloadReconciliationPdf'
    ]);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);
    api.getReconciliation.and.returnValue(of(emptyData));

    await TestBed.configureTestingModule({
      imports: [ReconciliationReportComponent],
      providers: [
        { provide: ReportsApiService, useValue: api },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ReconciliationReportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('ReportsPdf_ShouldNotExport_WhenNoData', () => {
    component.data = emptyData;

    component.exportPdf();

    expect(api.downloadReconciliationPdf).not.toHaveBeenCalled();
    expect(component.exportMessage).toBe('No hay informacion para exportar.');
    expect(notifications.error).toHaveBeenCalledWith('No hay informacion para exportar.');
  });

  it('ReportsPdf_ShouldShowNoDataMessage_WhenNoData', () => {
    component.data = emptyData;

    component.exportPdf();

    expect(component.exportMessage).toBe('No hay informacion para exportar.');
  });

  it('ReconciliationReport_ShouldGenerateNonEmptyPdf_WhenDataExists', async () => {
    spyOn<any>(component, 'getInvalidPdfMessage').and.resolveTo(null);
    spyOn(window.URL, 'createObjectURL').and.returnValue('blob:reconciliation');
    spyOn(window.URL, 'revokeObjectURL');
    const originalCreateElement = document.createElement.bind(document);
    const anchor = originalCreateElement('a') as HTMLAnchorElement;
    spyOn(anchor, 'click');
    const createElementSpy = spyOn(document, 'createElement').and.callFake((tagName: string, options?: ElementCreationOptions) => {
      return tagName.toLowerCase() === 'a' ? anchor : originalCreateElement(tagName, options);
    });
    component.data = {
      ...emptyData,
      totals: { ...emptyData.totals, sentCount: 1, sentAmount: 1000 }
    };
    const pdfBytes = new Blob(['%PDF-1.4\n', 'x'.repeat(1024)], { type: 'application/pdf' });
    api.downloadReconciliationPdf.and.returnValue(of(new HttpResponse({
      body: pdfBytes,
      headers: new HttpHeaders({ 'content-type': 'application/pdf' })
    })));

    component.exportPdf();
    await new Promise((resolve) => setTimeout(resolve, 25));
    createElementSpy.and.callThrough();

    expect(api.downloadReconciliationPdf).toHaveBeenCalled();
    expect(window.URL.createObjectURL).toHaveBeenCalled();
    expect(anchor.click).toHaveBeenCalled();
    expect(notifications.success).toHaveBeenCalledWith('PDF exportado correctamente.');
  });

  it('ReportExport_ShouldNotShowSuccess_WhenPdfWasNotGenerated', async () => {
    component.data = {
      ...emptyData,
      totals: { ...emptyData.totals, sentCount: 1, sentAmount: 1000 }
    };
    api.downloadReconciliationPdf.and.returnValue(of(new HttpResponse({
      body: new Blob([], { type: 'application/pdf' }),
      headers: new HttpHeaders({ 'content-type': 'application/pdf' })
    })));

    component.exportPdf();
    await new Promise((resolve) => setTimeout(resolve));

    expect(notifications.success).not.toHaveBeenCalled();
    expect(notifications.error).toHaveBeenCalledWith('No hay informacion para exportar.');
  });
});
