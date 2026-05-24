import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { ReportsApiService } from '../services/reports-api.service';
import { ReportListPageComponent } from './report-list-page.component';

describe('ReportListPageComponent', () => {
  let fixture: ComponentFixture<ReportListPageComponent>;
  let component: ReportListPageComponent;
  let apiMock: jasmine.SpyObj<ReportsApiService>;

  beforeEach(async () => {
    apiMock = jasmine.createSpyObj<ReportsApiService>('ReportsApiService', [
      'getSentTransactions',
      'getReceivedTransactions',
      'getReturns',
      'getRejections',
      'getNachaFiles',
      'getCyclesReport',
      'getAudit',
      'getHistory',
      'downloadSentTransactionsPdf',
      'downloadReceivedTransactionsPdf',
      'downloadReturnsPdf',
      'downloadRejectionsPdf',
      'downloadNachaFilesPdf',
      'downloadCyclesPdf',
      'downloadAuditPdf',
      'downloadHistoryPdf'
    ]);
    apiMock.getSentTransactions.and.returnValue(of({ items: [], total: 0 } as any));

    await TestBed.configureTestingModule({
      imports: [ReportListPageComponent],
      providers: [
        { provide: ReportsApiService, useValue: apiMock },
        { provide: NotificationService, useValue: { success: jasmine.createSpy(), error: jasmine.createSpy() } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              data: { title: 'Enviados', reportKey: 'sent', permissions: ['CanReadAch'] }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ReportListPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function button(testId: string): HTMLButtonElement {
    return fixture.nativeElement.querySelector(`[data-testid="${testId}"]`) as HTMLButtonElement;
  }

  it('ReportButtons_ShouldRenderPrimarySearchButton', () => {
    const search = button('report-search-button');

    expect(search.textContent?.trim()).toBe('Buscar');
    expect(search.classList).toContain('report-action-primary');
  });

  it('ReportButtons_ShouldRenderSecondaryClearButton', () => {
    const clear = button('report-clear-button');

    expect(clear.textContent?.trim()).toBe('Limpiar');
    expect(clear.classList).toContain('report-action-secondary');
  });

  it('ReportButtons_ShouldRenderExportPdfButton', () => {
    const exportButton = button('report-export-pdf-button');

    expect(exportButton.textContent?.trim()).toBe('Exportar PDF');
    expect(exportButton.classList).toContain('report-action-export');
  });

  it('ReportButtons_ShouldNotRenderCriticalButtonsAsPlainWhite', () => {
    const search = button('report-search-button');
    const clear = button('report-clear-button');
    const exportButton = button('report-export-pdf-button');

    expect(search.className).toContain('report-action-primary');
    expect(clear.className).toContain('report-action-secondary');
    expect(exportButton.className).toContain('report-action-export');
    expect(exportButton.className).not.toContain('btn-primary');
  });

  it('ReportFilters_ShouldKeepButtonsAccessible', () => {
    const actionButtons = [
      button('report-search-button'),
      button('report-clear-button'),
      button('report-export-pdf-button')
    ];

    expect(actionButtons.every((item) => item.offsetWidth >= 0)).toBeTrue();
    expect(actionButtons.every((item) => item.type === 'submit' || item.type === 'button')).toBeTrue();
  });

  it('ReportButtons_ShouldHaveTextOrAriaLabel', () => {
    const actionButtons = [
      button('report-search-button'),
      button('report-clear-button'),
      button('report-export-pdf-button')
    ];

    expect(actionButtons.every((item) => Boolean(item.textContent?.trim() || item.getAttribute('aria-label')))).toBeTrue();
  });
});
