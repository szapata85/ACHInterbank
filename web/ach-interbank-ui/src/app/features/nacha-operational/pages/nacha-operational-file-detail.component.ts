import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { BehaviorSubject, EMPTY, catchError, finalize, forkJoin, of, switchMap } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import {
  IncomingNachaAddenda,
  IncomingNachaBatch,
  IncomingNachaFileDetail,
  IncomingNachaPage,
  IncomingNachaQueueDetail,
  IncomingNachaTransaction,
  IncomingNachaTransactionFilters,
  IncomingNachaValidation
} from '../models/incoming-nacha-command-center.models';
import {
  abbreviatedIdentifier,
  logicalServiceName,
  operationalTone,
  technicalErrorMessage
} from '../presentation/incoming-nacha-presentation';
import { IncomingNachaCommandCenterService } from '../services/incoming-nacha-command-center.service';

interface BatchRequest { page: number; pageSize: number; sortBy: string; sortDescending: boolean; search: string; }

@Component({
  selector: 'app-nacha-operational-file-detail',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterModule, SharedModule, MatButtonModule, MatCardModule,
    MatCheckboxModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule,
    MatProgressBarModule, MatSelectModule, MatSortModule, MatTableModule, MatTabsModule, MatTooltipModule
  ],
  templateUrl: './nacha-operational-file-detail.component.html',
  styleUrls: ['./nacha-operational-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaOperationalFileDetailComponent implements OnInit {
  private readonly api = inject(IncomingNachaCommandCenterService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly batchesRequest$ = new BehaviorSubject<BatchRequest>({ page: 1, pageSize: 10, sortBy: 'batchNumber', sortDescending: false, search: '' });
  private readonly transactionsRequest$ = new BehaviorSubject<IncomingNachaTransactionFilters>({ page: 1, pageSize: 10, sortBy: 'traceNumber', sortDescending: false });

  @ViewChild('transactionHeading') transactionHeading?: ElementRef<HTMLElement>;

  readonly sectionKeys = ['resumen', 'validaciones', 'lotes', 'transacciones', 'procesamiento'];
  readonly batchColumns = ['batchNumber', 'serviceClass', 'company', 'description', 'effectiveDate', 'transactions', 'debit', 'credit', 'action'];
  readonly transactionColumns = ['trace', 'code', 'account', 'amount', 'classification', 'technical', 'outcome', 'result', 'service', 'attempts', 'processedAt', 'action'];
  readonly processingColumns = ['transaction', 'status', 'scheduled', 'attempts', 'nextAttempt', 'service', 'error'];
  readonly processingStatuses = [
    { value: 'Scheduled', label: 'Programado' }, { value: 'Processing', label: 'Procesando' },
    { value: 'Completed', label: 'Procesado' }, { value: 'RetryPending', label: 'Pendiente de reintento' },
    { value: 'TechnicalFailed', label: 'Error técnico' }
  ];
  readonly businessOutcomes = [
    { value: 'Successful', label: 'Exitoso' }, { value: 'Rejected', label: 'Rechazado' },
    { value: 'Returned', label: 'Devuelto' }, { value: 'PendingResponse', label: 'Pendiente de respuesta' },
    { value: 'NotProcessed', label: 'No procesado' }
  ];
  readonly progressStages = [
    { code: 'Received', label: 'Recibido' },
    { code: 'PreValidating', label: 'Validación inicial' },
    { code: 'Decrypting', label: 'Descifrado' },
    { code: 'HeaderParsing', label: 'Lectura del encabezado' },
    { code: 'ValidatingHeader', label: 'Validación de fecha' },
    { code: 'ValidatingCycle', label: 'Validación del ciclo' },
    { code: 'Parsing', label: 'Lectura del contenido' },
    { code: 'ValidatingContent', label: 'Validación del contenido' },
    { code: 'Persisting', label: 'Almacenamiento de información' },
    { code: 'Persisted', label: 'Carga completada' }
  ];

  readonly transactionFilters = this.fb.group({
    batchId: [null as number | null],
    search: [''],
    transactionCode: [''],
    processingStatus: [''],
    businessOutcome: [''],
    resultCode: [''],
    hasAddenda: [false],
    hasTechnicalError: [false]
  });

  ingestionId = '';
  detail?: IncomingNachaFileDetail;
  validations: IncomingNachaValidation[] = [];
  batches: IncomingNachaBatch[] = [];
  transactions: IncomingNachaTransaction[] = [];
  selectedTransaction?: IncomingNachaTransaction;
  addendas: IncomingNachaAddenda[] = [];
  queueDetail?: IncomingNachaQueueDetail;
  selectedTabIndex = 0;
  batchTotal = 0;
  batchPageIndex = 0;
  batchPageSize = 10;
  transactionTotal = 0;
  transactionPageIndex = 0;
  transactionPageSize = 10;
  loadingDetail = false;
  loadingValidations = false;
  loadingBatches = false;
  loadingTransactions = false;
  loadingTransactionDetail = false;
  detailError = '';
  validationsError = '';
  batchesError = '';
  transactionsError = '';
  transactionDetailError = '';

  ngOnInit(): void {
    const section = this.route.snapshot.queryParamMap.get('seccion') ?? 'resumen';
    this.selectedTabIndex = Math.max(0, this.sectionKeys.indexOf(section));

    this.route.paramMap.pipe(
      switchMap((params) => {
        const id = params.get('fileId');
        if (!id) {
          this.detailError = 'El identificador del archivo no es válido.';
          return EMPTY;
        }
        this.ingestionId = id;
        this.loadAllSections();
        return EMPTY;
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();

    this.batchesRequest$.pipe(
      switchMap((request) => {
        if (!this.ingestionId) return EMPTY;
        this.loadingBatches = true;
        this.batchesError = '';
        return this.api.getBatches(this.ingestionId, request.page, request.pageSize, request.sortBy, request.sortDescending, request.search).pipe(
          catchError(() => { this.batchesError = 'No fue posible consultar los lotes del archivo.'; return of(null); }),
          finalize(() => {
            this.loadingBatches = false;
            this.cdr.markForCheck();
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((page) => this.assignBatches(page));

    this.transactionsRequest$.pipe(
      switchMap((request) => {
        if (!this.ingestionId) return EMPTY;
        this.loadingTransactions = true;
        this.transactionsError = '';
        return this.api.getTransactions(this.ingestionId, request).pipe(
          catchError(() => { this.transactionsError = 'No fue posible consultar las transacciones del archivo.'; return of(null); }),
          finalize(() => {
            this.loadingTransactions = false;
            this.cdr.markForCheck();
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((page) => this.assignTransactions(page));
  }

  refresh(): void { this.loadAllSections(); }

  backToList(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('retorno');
    void this.router.navigateByUrl(returnUrl?.startsWith('/incoming-nacha-command-center') ? returnUrl : '/incoming-nacha-command-center');
  }

  tabChanged(index: number): void {
    this.selectedTabIndex = index;
    void this.router.navigate([], { relativeTo: this.route, queryParams: { seccion: this.sectionKeys[index] }, queryParamsHandling: 'merge', replaceUrl: true });
  }

  retryValidations(): void { this.loadValidations(); }
  retryBatches(): void { this.batchesRequest$.next(this.batchesRequest$.value); }
  retryTransactions(): void { this.transactionsRequest$.next(this.transactionsRequest$.value); }

  batchPageChanged(event: PageEvent): void {
    this.batchPageIndex = event.pageIndex;
    this.batchPageSize = event.pageSize;
    this.batchesRequest$.next({ ...this.batchesRequest$.value, page: event.pageIndex + 1, pageSize: event.pageSize });
  }

  batchSortChanged(sort: Sort): void {
    this.batchesRequest$.next({ ...this.batchesRequest$.value, page: 1, sortBy: sort.active || 'batchNumber', sortDescending: sort.direction === 'desc' });
  }

  filterByBatch(batch: IncomingNachaBatch): void {
    this.selectedTabIndex = 3;
    this.transactionFilters.patchValue({ batchId: batch.id });
    this.applyTransactionFilters();
    this.tabChanged(3);
  }

  applyTransactionFilters(): void {
    const value = this.transactionFilters.getRawValue();
    this.transactionsRequest$.next({
      page: 1,
      pageSize: this.transactionPageSize,
      batchId: value.batchId ?? undefined,
      search: value.search?.trim() || undefined,
      transactionCode: value.transactionCode?.trim() || undefined,
      processingStatus: value.processingStatus || undefined,
      businessOutcome: value.businessOutcome || undefined,
      resultCode: value.resultCode?.trim().toUpperCase() || undefined,
      hasAddenda: value.hasAddenda || undefined,
      hasTechnicalError: value.hasTechnicalError || undefined,
      sortBy: 'traceNumber',
      sortDescending: false
    } as IncomingNachaTransactionFilters);
  }

  clearTransactionFilters(): void {
    this.transactionFilters.reset({ batchId: null, search: '', transactionCode: '', processingStatus: '', businessOutcome: '', resultCode: '', hasAddenda: false, hasTechnicalError: false });
    this.applyTransactionFilters();
  }

  transactionPageChanged(event: PageEvent): void {
    this.transactionPageIndex = event.pageIndex;
    this.transactionPageSize = event.pageSize;
    this.transactionsRequest$.next({ ...this.transactionsRequest$.value, page: event.pageIndex + 1, pageSize: event.pageSize });
  }

  transactionSortChanged(sort: Sort): void {
    this.transactionsRequest$.next({ ...this.transactionsRequest$.value, page: 1, sortBy: sort.active || 'traceNumber', sortDescending: sort.direction === 'desc' });
  }

  openTransaction(transaction: IncomingNachaTransaction): void {
    this.selectedTransaction = transaction;
    this.addendas = [];
    this.queueDetail = undefined;
    this.transactionDetailError = '';
    this.loadingTransactionDetail = true;
    void this.router.navigate([], { relativeTo: this.route, queryParams: { transaccion: transaction.id, seccion: 'transacciones' }, queryParamsHandling: 'merge', replaceUrl: true });

    const queueRequest = transaction.dispatchQueueId
      ? this.api.getQueueDetail(transaction.dispatchQueueId).pipe(catchError(() => of(undefined)))
      : of(undefined);
    forkJoin({
      addendas: this.api.getAddendas(this.ingestionId, transaction.id).pipe(catchError(() => of([]))),
      queue: queueRequest
    }).pipe(finalize(() => {
      this.loadingTransactionDetail = false;
      this.cdr.markForCheck();
    }), takeUntilDestroyed(this.destroyRef))
      .subscribe(({ addendas, queue }) => {
        this.addendas = addendas;
        this.queueDetail = queue;
        if (!transaction.dispatchQueueId) this.transactionDetailError = 'La transacción aún no tiene programación de procesamiento asociada.';
        this.cdr.markForCheck();
        queueMicrotask(() => this.transactionHeading?.nativeElement.focus());
      });
  }

  closeTransaction(): void {
    this.selectedTransaction = undefined;
    this.addendas = [];
    this.queueDetail = undefined;
    void this.router.navigate([], { relativeTo: this.route, queryParams: { transaccion: null }, queryParamsHandling: 'merge', replaceUrl: true });
  }

  copyCorrelation(value: string): void {
    if (navigator.clipboard) void navigator.clipboard.writeText(value);
  }

  tone(value: string | null | undefined): string { return `status status--${operationalTone(value)}`; }
  serviceName(value: string | null | undefined): string { return logicalServiceName(value); }
  technicalMessage(code: string, message: string): string { return technicalErrorMessage(code, message); }
  abbreviated(value: string | null | undefined): string { return abbreviatedIdentifier(value); }

  progressState(code: string): string {
    const current = this.detail?.stageCode ?? 'Received';
    if (current === 'Rejected') return code === this.previousStageForTerminal() ? 'rejected' : 'pending';
    if (current === 'Failed') return code === this.previousStageForTerminal() ? 'failed' : 'pending';
    const currentIndex = this.progressStages.findIndex((stage) => stage.code === current);
    const stageIndex = this.progressStages.findIndex((stage) => stage.code === code);
    if (stageIndex < currentIndex || current === 'Persisted') return 'completed';
    if (stageIndex === currentIndex) return 'current';
    return 'pending';
  }

  private previousStageForTerminal(): string {
    const events = this.detail?.events ?? [];
    return events.find((event) => this.progressStages.some((stage) => event.eventType.includes(stage.code)))?.eventType ?? 'Received';
  }

  private loadAllSections(): void {
    this.loadDetail();
    this.loadValidations();
    this.batchesRequest$.next(this.batchesRequest$.value);
    this.transactionsRequest$.next(this.transactionsRequest$.value);
  }

  private loadDetail(): void {
    this.loadingDetail = true;
    this.detailError = '';
    this.api.getFile(this.ingestionId).pipe(
      catchError((error: { status?: number }) => {
        this.detailError = error.status === 404
          ? 'No se encontró el archivo solicitado.'
          : error.status === 403
            ? 'No tiene permiso para consultar este archivo.'
            : 'No fue posible consultar la información del archivo. Revise su conexión e intente nuevamente.';
        return of(undefined);
      }),
      finalize(() => {
        this.loadingDetail = false;
        this.cdr.markForCheck();
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((detail) => {
      this.detail = detail;
      this.cdr.markForCheck();
    });
  }

  private loadValidations(): void {
    this.loadingValidations = true;
    this.validationsError = '';
    this.api.getValidations(this.ingestionId).pipe(
      catchError(() => { this.validationsError = 'No fue posible consultar las validaciones del archivo.'; return of([]); }),
      finalize(() => {
        this.loadingValidations = false;
        this.cdr.markForCheck();
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((validations) => {
      this.validations = validations;
      this.cdr.markForCheck();
    });
  }

  private assignBatches(page: IncomingNachaPage<IncomingNachaBatch> | null): void {
    if (!page) return;
    this.batches = page.items;
    this.batchTotal = page.totalItems;
    this.batchPageIndex = page.page - 1;
    this.batchPageSize = page.pageSize;
    this.cdr.markForCheck();
  }

  private assignTransactions(page: IncomingNachaPage<IncomingNachaTransaction> | null): void {
    if (!page) return;
    this.transactions = page.items;
    this.transactionTotal = page.totalItems;
    this.transactionPageIndex = page.page - 1;
    this.transactionPageSize = page.pageSize;
    this.cdr.markForCheck();
  }
}
