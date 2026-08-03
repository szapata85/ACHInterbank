import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { catchError, distinctUntilChanged, finalize, map, of, switchMap, tap } from 'rxjs';
import { OutgoingTransactionMonitoringApiService } from './outgoing-transaction-monitoring-api.service';
import { OutgoingMonitoringDetail } from './outgoing-transaction-monitoring.models';

@Component({
  selector: 'app-outgoing-transaction-monitoring-detail',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatCardModule, MatExpansionModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './outgoing-transaction-monitoring-detail.component.html',
  styleUrl: './outgoing-transaction-monitoring-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OutgoingTransactionMonitoringDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(OutgoingTransactionMonitoringApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly forbidden = signal(false);
  readonly error = signal(false);
  readonly detail = signal<OutgoingMonitoringDetail | null>(null);

  ngOnInit(): void {
    this.route.paramMap.pipe(
      map(params => Number(params.get('id'))),
      distinctUntilChanged(),
      tap(() => this.resetState()),
      switchMap(id => this.api.getDetail(id).pipe(
        catchError((response: HttpErrorResponse) => {
          this.applyError(response);
          return of(null);
        }),
        finalize(() => this.loading.set(false))
      )),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(detail => this.detail.set(detail));
  }

  back(): void { void this.router.navigate(['/transactions/outgoing-monitoring']); }
  reload(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.resetState();
    this.api.getDetail(id).pipe(
      catchError((response: HttpErrorResponse) => { this.applyError(response); return of(null); }),
      finalize(() => this.loading.set(false)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(value => this.detail.set(value));
  }

  eventIcon(stageCode: string): string {
    const icons: Record<string, string> = {
      Creation: 'add_circle', Classification: 'rule', CycleAssignment: 'schedule', CycleReassignment: 'update',
      Preparation: 'pending_actions', MonetaryIntegration: 'account_balance', FileInclusion: 'description',
      FileProtection: 'lock', Transmission: 'send', Acknowledgement: 'mark_email_read', Acceptance: 'check_circle',
      Certification: 'verified', Rejection: 'cancel', Return: 'assignment_return', DifferentialResponse: 'sync_alt',
      TechnicalError: 'error', ManualReview: 'person_search'
    };
    return icons[stageCode] ?? 'radio_button_checked';
  }

  private resetState(): void {
    this.loading.set(true);
    this.error.set(false);
    this.notFound.set(false);
    this.forbidden.set(false);
  }

  private applyError(response: HttpErrorResponse): void {
    this.notFound.set(response.status === 404);
    this.forbidden.set(response.status === 401 || response.status === 403);
    this.error.set(!this.notFound() && !this.forbidden());
  }
}
