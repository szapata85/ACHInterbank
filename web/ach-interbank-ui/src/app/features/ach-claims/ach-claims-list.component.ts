import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { catchError, finalize, of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { DateFormatPipe } from '../../shared/pipes/date-format.pipe';
import { DestinationInstitution } from '../../core/models/financial-institution.model';
import { FinancialInstitutionsApiService } from '../../core/services/financial-institutions-api.service';
import { AchClaimsApiService } from './ach-claims-api.service';
import { claimsErrorMessage } from './ach-claims-error.util';
import {
  ClaimCaseListItem,
  ClaimCaseQuery,
  ClaimCaseQueryResult,
  ClaimCaseStatus,
  ClaimCauseCode
} from './ach-claims.models';
import { claimCauseLabel, claimStatusLabel } from './ach-claims.presentation';
import { CreateDev14DialogComponent } from './dialogs/create-dev14-dialog.component';

type ClaimFiltersForm = FormGroup<{
  status: FormControl<ClaimCaseStatus | null>;
  causeCode: FormControl<ClaimCauseCode | null>;
  transactionId: FormControl<number | null>;
  participantId: FormControl<number | null>;
  from: FormControl<Date | null>;
  to: FormControl<Date | null>;
}>;

@Component({
  selector: 'app-ach-claims-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    DateFormatPipe
  ],
  templateUrl: './ach-claims-list.component.html',
  styleUrl: './ach-claims-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchClaimsListComponent implements OnInit {
  private readonly api = inject(AchClaimsApiService);
  private readonly auth = inject(AuthService);
  private readonly institutionsApi = inject(FinancialInstitutionsApiService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly error = signal('');
  readonly page = signal<ClaimCaseQueryResult>({ items: [], pageNumber: 1, pageSize: 25, totalItems: 0 });
  readonly institutions = signal<DestinationInstitution[]>([]);
  readonly canCreate = this.auth.hasPermission('Claims.CreateDev14');
  readonly statuses = [ClaimCaseStatus.Open, ClaimCaseStatus.Resolved, ClaimCaseStatus.Expired];
  readonly causes = [ClaimCauseCode.Dev14];
  readonly displayedColumns = [
    'id', 'cause', 'status', 'filedAt', 'deadlineAt', 'requestingParticipant',
    'respondingParticipant', 'transactions', 'externalReference', 'actions'
  ];

  currentPage = 1;
  currentPageSize = 25;

  readonly form: ClaimFiltersForm = new FormGroup({
    status: new FormControl<ClaimCaseStatus | null>(null),
    causeCode: new FormControl<ClaimCauseCode | null>(null),
    transactionId: new FormControl<number | null>(null),
    participantId: new FormControl<number | null>(null),
    from: new FormControl<Date | null>(null),
    to: new FormControl<Date | null>(null)
  });

  ngOnInit(): void {
    this.loadInstitutions();
    this.load();
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.load();
  }

  clearFilters(): void {
    this.form.reset();
    this.currentPage = 1;
    this.currentPageSize = 25;
    this.load();
  }

  pageChanged(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.currentPageSize = event.pageSize;
    this.load();
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(CreateDev14DialogComponent, { width: '620px', maxWidth: '95vw' });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(created => {
      if (created) {
        void this.router.navigate(['/ach-claims', created.id]);
      }
    });
  }

  viewDetail(item: ClaimCaseListItem): void {
    void this.router.navigate(['/ach-claims', item.id]);
  }

  statusLabel(status: ClaimCaseStatus): string { return claimStatusLabel(status); }
  causeLabel(cause: ClaimCauseCode): string { return claimCauseLabel(cause, true); }
  trackById(_: number, item: ClaimCaseListItem): number { return item.id; }

  private loadInstitutions(): void {
    this.institutionsApi.getAll(false).pipe(
      catchError(() => of([] as DestinationInstitution[])),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(items => this.institutions.set(items));
  }

  private load(): void {
    this.loading.set(true);
    this.error.set('');
    const query = this.buildQuery();
    this.api.query(query).pipe(
      catchError(err => {
        this.error.set(claimsErrorMessage(err, 'No fue posible consultar las reclamaciones en este momento.'));
        return of({ items: [], pageNumber: query.pageNumber, pageSize: query.pageSize, totalItems: 0 });
      }),
      finalize(() => this.loading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => this.page.set(result));
  }

  private buildQuery(): ClaimCaseQuery {
    const value = this.form.getRawValue();
    return {
      ...(value.status != null ? { status: value.status } : {}),
      ...(value.causeCode != null ? { causeCode: value.causeCode } : {}),
      ...(value.transactionId != null && value.transactionId > 0 ? { transactionId: value.transactionId } : {}),
      ...(value.participantId != null && value.participantId > 0 ? { participantId: value.participantId } : {}),
      ...(value.from ? { from: startOfLocalDay(value.from) } : {}),
      ...(value.to ? { to: endOfLocalDay(value.to) } : {}),
      pageNumber: this.currentPage,
      pageSize: this.currentPageSize
    };
  }
}

function startOfLocalDay(value: Date): string {
  return bogotaDateTime(value, '00:00:00.000');
}

function endOfLocalDay(value: Date): string {
  return bogotaDateTime(value, '23:59:59.999');
}

function bogotaDateTime(value: Date, time: string): string {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}T${time}-05:00`;
}
