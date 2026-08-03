import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
  styleUrl: './outgoing-transaction-monitoring-detail.component.scss'
})
export class OutgoingTransactionMonitoringDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(OutgoingTransactionMonitoringApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly error = signal(false);
  readonly detail = signal<OutgoingMonitoringDetail | null>(null);

  ngOnInit(): void {
    this.route.paramMap.pipe(
      map(params => Number(params.get('id'))),
      distinctUntilChanged(),
      tap(() => { this.loading.set(true); this.error.set(false); this.notFound.set(false); }),
      switchMap(id => this.api.getDetail(id).pipe(
        catchError(response => {
          this.notFound.set(response?.status === 404);
          this.error.set(response?.status !== 404);
          return of(null);
        }),
        finalize(() => this.loading.set(false))
      )),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(detail => this.detail.set(detail));
  }

  back(): void { void this.router.navigate(['/transactions/outgoing-monitoring']); }
  reload(): void { const id = Number(this.route.snapshot.paramMap.get('id')); this.loading.set(true); this.api.getDetail(id).pipe(finalize(() => this.loading.set(false)), takeUntilDestroyed(this.destroyRef)).subscribe({ next: value => { this.detail.set(value); this.error.set(false); }, error: () => this.error.set(true) }); }

  eventIcon(stageCode: string): string {
    const icons: Record<string, string> = { Creation: 'add_circle', Classification: 'rule', CycleAssignment: 'schedule', MonetaryIntegration: 'account_balance', FileInclusion: 'description', Acceptance: 'check_circle', Certification: 'verified', Return: 'assignment_return', DifferentialResponse: 'sync_alt' };
    return icons[stageCode] ?? 'radio_button_checked';
  }
}
