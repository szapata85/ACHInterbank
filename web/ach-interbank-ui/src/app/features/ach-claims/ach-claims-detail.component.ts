import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { catchError, distinctUntilChanged, finalize, map, of, switchMap, tap } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { CurrencyColPipe } from '../../shared/pipes/currency-col.pipe';
import { DateFormatPipe } from '../../shared/pipes/date-format.pipe';
import { AchClaimsApiService } from './ach-claims-api.service';
import { claimsErrorMessage } from './ach-claims-error.util';
import {
  ClaimCaseDetail,
  ClaimCaseEventType,
  ClaimCaseStatus,
  ClaimCauseCode,
  ClaimTransaction,
  AchTransactionType
} from './ach-claims.models';
import { claimCauseLabel, claimEventLabel, claimStatusLabel, transactionTypeLabel } from './ach-claims.presentation';
import { ExpireClaimDialogComponent } from './dialogs/expire-claim-dialog.component';
import { RecordProgressDialogComponent } from './dialogs/record-progress-dialog.component';
import { ResolveClaimDialogComponent } from './dialogs/resolve-claim-dialog.component';

@Component({
  selector: 'app-ach-claims-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    CurrencyColPipe,
    DateFormatPipe
  ],
  templateUrl: './ach-claims-detail.component.html',
  styleUrl: './ach-claims-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchClaimsDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(AchClaimsApiService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly error = signal('');
  readonly detail = signal<ClaimCaseDetail | null>(null);
  readonly canProgress = this.auth.hasPermission('Claims.Progress');
  readonly canResolve = this.auth.hasPermission('Claims.Resolve');
  readonly canExpire = this.auth.hasPermission('Claims.Expire');
  readonly transactionColumns = ['id', 'trace', 'externalReference', 'amount', 'type', 'source', 'destination'];

  ngOnInit(): void {
    this.route.paramMap.pipe(
      map(params => Number(params.get('id'))),
      distinctUntilChanged(),
      tap(() => { this.loading.set(true); this.error.set(''); }),
      switchMap(id => this.api.getById(id).pipe(
        catchError(err => {
          this.error.set(claimsErrorMessage(err, 'No fue posible consultar la reclamación.'));
          return of(null);
        }),
        finalize(() => this.loading.set(false))
      )),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(detail => this.detail.set(detail));
  }

  back(): void { void this.router.navigate(['/ach-claims']); }

  reload(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.loading.set(true);
    this.error.set('');
    this.api.getById(id).pipe(
      catchError(err => {
        this.error.set(claimsErrorMessage(err, 'No fue posible consultar la reclamación.'));
        return of(null);
      }),
      finalize(() => this.loading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(detail => this.detail.set(detail));
  }

  recordProgress(): void {
    const current = this.detail();
    if (!current || current.status !== ClaimCaseStatus.Open || !this.canProgress) return;
    this.dialog.open(RecordProgressDialogComponent, {
      width: '580px', maxWidth: '95vw', data: { id: current.id }
    }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(updated => {
      if (updated) this.detail.set(updated);
    });
  }

  resolve(): void {
    const current = this.detail();
    if (!current || current.status !== ClaimCaseStatus.Open || !this.canResolve) return;
    this.dialog.open(ResolveClaimDialogComponent, {
      width: '580px', maxWidth: '95vw', data: { id: current.id }
    }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(updated => {
      if (updated) this.detail.set(updated);
    });
  }

  expire(): void {
    const current = this.detail();
    if (!current || current.status !== ClaimCaseStatus.Open || !this.canExpire) return;
    this.dialog.open(ExpireClaimDialogComponent, {
      width: '520px', maxWidth: '95vw', data: { id: current.id }
    }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(updated => {
      if (updated) this.detail.set(updated);
    });
  }

  isOpen(detail: ClaimCaseDetail): boolean { return detail.status === ClaimCaseStatus.Open; }
  statusLabel(status: ClaimCaseStatus): string { return claimStatusLabel(status); }
  causeLabel(cause: ClaimCauseCode): string { return claimCauseLabel(cause); }
  eventLabel(event: ClaimCaseEventType): string { return claimEventLabel(event); }
  typeLabel(type: AchTransactionType): string { return transactionTypeLabel(type); }
  trackTransaction(_: number, transaction: ClaimTransaction): number { return transaction.id; }
}
