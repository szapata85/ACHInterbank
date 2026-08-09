import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatRadioModule } from '@angular/material/radio';
import { MatTableModule } from '@angular/material/table';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import {
  IncomingNachaOrphan,
  IncomingNachaOrphanCandidate
} from '../models/incoming-nacha-command-center.models';
import { IncomingNachaCommandCenterApiService } from '../services/incoming-nacha-command-center-api.service';

@Component({
  selector: 'app-incoming-nacha-orphans-page',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterModule, SharedModule, MatButtonModule, MatCardModule,
    MatCheckboxModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressBarModule,
    MatRadioModule, MatTableModule
  ],
  templateUrl: './incoming-nacha-orphans-page.component.html',
  styleUrls: ['./incoming-nacha-orphans-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class IncomingNachaOrphansPageComponent implements OnInit {
  private readonly api = inject(IncomingNachaCommandCenterApiService);
  private readonly fb = inject(FormBuilder);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly displayedColumns = ['archivo', 'fecha', 'valor', 'causal', 'rastreo', 'estado', 'accion'];
  readonly candidateColumns = ['seleccion', 'transaccion', 'rastreo', 'fecha', 'valor', 'estado', 'compatibilidad'];
  readonly searchForm = this.fb.group({ search: [''] });
  readonly resolutionForm = this.fb.group({
    candidateId: [null as number | null, Validators.required],
    justification: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(500)]],
    comment: ['', Validators.maxLength(500)],
    confirmed: [false, Validators.requiredTrue]
  });

  orphans: IncomingNachaOrphan[] = [];
  selected?: IncomingNachaOrphan;
  candidates: IncomingNachaOrphanCandidate[] = [];
  loading = false;
  candidateLoading = false;
  resolving = false;
  error = '';
  candidateError = '';

  ngOnInit(): void {
    this.loadOrphans();
  }

  loadOrphans(): void {
    this.loading = true;
    this.error = '';
    const search = this.searchForm.controls.search.value?.trim();
    const params: Record<string, string | number> = { page: 1, pageSize: 50 };
    if (search) params['search'] = search;
    this.api.getOrphans(params).pipe(finalize(() => {
      this.loading = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: page => this.orphans = page.items,
      error: error => this.error = error?.error?.message ?? 'No fue posible consultar las devoluciones sin relación.'
    });
  }

  selectOrphan(orphan: IncomingNachaOrphan): void {
    this.selected = orphan;
    this.candidates = [];
    this.candidateError = '';
    this.resolutionForm.reset({ candidateId: null, justification: '', comment: '', confirmed: false });
    this.loadCandidates();
  }

  loadCandidates(): void {
    if (!this.selected) return;
    this.candidateLoading = true;
    this.candidateError = '';
    this.api.getOrphanCandidates(this.selected.id).pipe(finalize(() => {
      this.candidateLoading = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: candidates => {
        this.candidates = candidates;
        if (candidates.length === 1 && candidates[0].isCompatible) {
          this.resolutionForm.controls.candidateId.setValue(candidates[0].achTransactionId);
        }
      },
      error: error => this.candidateError = error?.error?.message ?? 'No fue posible consultar las transacciones candidatas.'
    });
  }

  resolve(): void {
    this.resolutionForm.markAllAsTouched();
    if (!this.selected || this.resolutionForm.invalid || this.resolving) return;
    const value = this.resolutionForm.getRawValue();
    const candidate = this.candidates.find(x => x.achTransactionId === value.candidateId);
    if (!candidate?.isCompatible) {
      this.notifications.error('Seleccione una transacción compatible antes de confirmar la relación.');
      return;
    }

    this.resolving = true;
    this.api.resolveOrphan(this.selected.id, {
      achTransactionId: candidate.achTransactionId,
      justification: value.justification!.trim(),
      comment: value.comment?.trim() ?? '',
      correlationId: crypto.randomUUID()
    }).pipe(finalize(() => {
      this.resolving = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: result => {
        this.notifications.success(result.isIdempotentReplay
          ? 'La relación ya estaba aplicada; no se generaron efectos adicionales.'
          : 'La devolución fue relacionada y aplicada correctamente.');
        this.api.getOrphan(this.selected!.id).subscribe(detail => {
          this.selected = detail;
          this.orphans = this.orphans.filter(x => x.id !== detail.id);
          this.cdr.markForCheck();
        });
      },
      error: error => this.notifications.error(error?.error?.message ?? 'No fue posible relacionar la devolución.')
    });
  }

  selectedCandidate(): IncomingNachaOrphanCandidate | undefined {
    return this.candidates.find(x => x.achTransactionId === this.resolutionForm.controls.candidateId.value);
  }
}
