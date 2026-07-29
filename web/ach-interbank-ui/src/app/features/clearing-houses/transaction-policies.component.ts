import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin, switchMap } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/services/auth.service';
import { ClearingHousesService } from './clearing-houses.service';
import { ClearingHouse } from './clearing-houses.models';
import { TransactionPoliciesService } from './transaction-policies.service';
import { CreateTransactionPolicyVersionRequest, PrenotificationMode, TransactionPolicy, TransactionPolicyPreview } from './transaction-policies.models';
import { TransactionTypeEnum } from '../transactions/transactions.types';

type PolicyForm = FormGroup<{
  transactionType: FormControl<TransactionTypeEnum | null>;
  prenotificationMode: FormControl<PrenotificationMode | null>;
  prenotificationLeadBusinessDays: FormControl<number | null>;
  effectiveFrom: FormControl<Date | null>;
  effectiveTo: FormControl<Date | null>;
  normativeSource: FormControl<string>;
  normativeReference: FormControl<string>;
  notes: FormControl<string>;
}>;

@Component({
  selector: 'app-transaction-policies', standalone: true, changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, DatePipe, MatButtonModule, MatCardModule, MatChipsModule, MatDatepickerModule, MatNativeDateModule, MatDividerModule, MatFormFieldModule, MatIconModule, MatInputModule, MatMenuModule, MatProgressSpinnerModule, MatSelectModule, MatSnackBarModule, MatTableModule, MatTooltipModule],
  templateUrl: './transaction-policies.component.html', styleUrl: './transaction-policies.component.scss'
})
export class TransactionPoliciesComponent {
  private readonly route = inject(ActivatedRoute); readonly router = inject(Router);
  private readonly houses = inject(ClearingHousesService); private readonly policiesApi = inject(TransactionPoliciesService);
  private readonly auth = inject(AuthService); private readonly snackBar = inject(MatSnackBar); private readonly cdr = inject(ChangeDetectorRef);
  readonly transactionTypes = TransactionTypeEnum;
  readonly displayedColumns = ['transactionType', 'mode', 'lead', 'from', 'to', 'state', 'reference', 'updated', 'actions'];
  readonly canManage = this.auth.hasPermission(['Config.Manage', 'CanManageAch']);
  readonly canRead = this.auth.hasPermission(['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch']);
  clearingHouse: ClearingHouse | null = null; policies: TransactionPolicy[] = []; loading = true; error = ''; saving = false;
  showForm = false; metadataPolicy: TransactionPolicy | null = null; previewResult: TransactionPolicyPreview | null = null;
  readonly form: PolicyForm = new FormGroup({
    transactionType: new FormControl<TransactionTypeEnum | null>(null, Validators.required),
    prenotificationMode: new FormControl<PrenotificationMode | null>(null, Validators.required),
    prenotificationLeadBusinessDays: new FormControl<number | null>(null, [Validators.min(0), Validators.max(365)]),
    effectiveFrom: new FormControl<Date | null>(null, Validators.required),
    effectiveTo: new FormControl<Date | null>(null),
    normativeSource: new FormControl('', [Validators.required, Validators.maxLength(250)]),
    normativeReference: new FormControl('', [Validators.required, Validators.maxLength(250)]),
    notes: new FormControl('', Validators.maxLength(2000))
  }, { validators: [group => {
    const from = group.get('effectiveFrom')?.value as Date | null; const to = group.get('effectiveTo')?.value as Date | null;
    return from && to && to < from ? { dateRange: true } : null;
  }] });

  constructor() {
    this.form.controls.prenotificationMode.valueChanges.subscribe(mode => {
      const lead = this.form.controls.prenotificationLeadBusinessDays;
      if (mode === 'Optional' || mode === 'NotApplicable') { lead.setValue(null, { emitEvent: false }); lead.disable({ emitEvent: false }); }
      else { lead.enable({ emitEvent: false }); }
    });
    this.route.paramMap.pipe(switchMap(params => {
      const id = Number(params.get('id')); if (!Number.isInteger(id) || id <= 0) { this.error = 'La cámara indicada no es válida.'; this.loading = false; return []; }
      return forkJoin({ house: this.houses.get(id), policies: this.policiesApi.list(id) });
    }), finalize(() => { this.loading = false; this.cdr.markForCheck(); })).subscribe({
      next: result => { this.clearingHouse = result.house; this.policies = result.policies; },
      error: err => this.error = err?.status === 404 ? 'La cámara solicitada no existe o ya no está disponible.' : 'No fue posible cargar las políticas transaccionales.'
    });
  }

  get currentDebit(): TransactionPolicy | undefined { return this.current(TransactionTypeEnum.Debit); }
  get currentCredit(): TransactionPolicy | undefined { return this.current(TransactionTypeEnum.Credit); }
  createVersion(): void { if (!this.canManage) return; this.metadataPolicy = null; this.previewResult = null; this.showForm = true; this.form.reset({ transactionType: TransactionTypeEnum.Debit, prenotificationMode: 'Mandatory', prenotificationLeadBusinessDays: null, effectiveFrom: new Date(), effectiveTo: null, normativeSource: '', normativeReference: '', notes: '' }); }
  editMetadata(policy: TransactionPolicy): void { if (!this.canManage) return; this.metadataPolicy = policy; this.showForm = true; this.form.reset({ transactionType: policy.transactionType, prenotificationMode: policy.prenotificationMode, prenotificationLeadBusinessDays: policy.prenotificationLeadBusinessDays, effectiveFrom: this.date(policy.effectiveFrom), effectiveTo: policy.effectiveTo ? this.date(policy.effectiveTo) : null, normativeSource: policy.normativeSource, normativeReference: policy.normativeReference, notes: policy.notes }); this.form.controls.transactionType.disable(); this.form.controls.prenotificationMode.disable(); this.form.controls.prenotificationLeadBusinessDays.disable(); this.form.controls.effectiveFrom.disable(); this.form.controls.effectiveTo.disable(); }
  cancel(): void { this.showForm = false; this.metadataPolicy = null; this.previewResult = null; this.form.enable(); }
  save(): void {
    this.form.markAllAsTouched(); if (!this.canManage || this.form.invalid || this.saving || !this.clearingHouse) return;
    this.saving = true; const value = this.form.getRawValue();
    const request$ = this.metadataPolicy ? this.policiesApi.updateMetadata(this.clearingHouse.id, this.metadataPolicy.id, { normativeSource: value.normativeSource.trim(), normativeReference: value.normativeReference.trim(), notes: value.notes.trim() || null }) : this.policiesApi.create(this.clearingHouse.id, this.payload(value));
    request$.pipe(finalize(() => { this.saving = false; this.cdr.markForCheck(); })).subscribe({ next: () => { this.snack('La política fue guardada.'); this.cancel(); this.reload(); }, error: err => this.snack(this.errorMessage(err), true) });
  }
  close(policy: TransactionPolicy): void { const until = window.prompt(`Cierre de vigencia para ${this.typeLabel(policy.transactionType)} (AAAA-MM-DD):`, this.iso(new Date())); if (!until || !this.clearingHouse || !this.canManage) return; this.policiesApi.close(this.clearingHouse.id, policy.id, until).subscribe({ next: () => { this.snack('Vigencia cerrada.'); this.reload(); }, error: err => this.snack(this.errorMessage(err), true) }); }
  activate(policy: TransactionPolicy): void { if (!this.clearingHouse || !this.canManage || !window.confirm('¿Activar esta versión futura?')) return; this.policiesApi.activate(this.clearingHouse.id, policy.id).subscribe({ next: () => { this.snack('Versión activada.'); this.reload(); }, error: err => this.snack(this.errorMessage(err), true) }); }
  preview(): void { const value = this.form.getRawValue(); if (!this.clearingHouse || !value.transactionType || !value.effectiveFrom) return; this.policiesApi.preview(this.clearingHouse.id, value.transactionType, this.iso(value.effectiveFrom)).subscribe({ next: result => { this.previewResult = result; this.cdr.markForCheck(); }, error: () => this.snack('No fue posible obtener la vista previa.', true) }); }
  status(policy: TransactionPolicy): string { const now = this.iso(new Date()); if (!policy.isActive) return 'Inactiva'; if (policy.effectiveFrom > now) return 'Futura'; if (policy.effectiveTo && policy.effectiveTo < now) return 'Histórica'; return 'Vigente'; }
  lead(policy?: TransactionPolicy): string { if (!policy || policy.prenotificationMode !== 'Mandatory') return 'No aplica'; return policy.prenotificationLeadBusinessDays == null ? 'Sin plazo mínimo documentado' : `${policy.prenotificationLeadBusinessDays} días hábiles`; }
  typeLabel(type: TransactionTypeEnum): string { return type === TransactionTypeEnum.Debit ? 'Débito' : 'Crédito'; }
  modeLabel(mode: PrenotificationMode): string { return mode === 'Mandatory' ? 'Prenotificación obligatoria' : mode === 'Optional' ? 'Prenotificación opcional' : 'No aplica'; }
  track = (_: number, policy: TransactionPolicy) => policy.id;
  private current(type: TransactionTypeEnum): TransactionPolicy | undefined { return this.policies.find(policy => policy.transactionType === type && this.status(policy) === 'Vigente'); }
  private reload(): void { if (!this.clearingHouse) return; this.policiesApi.list(this.clearingHouse.id).subscribe({ next: policies => { this.policies = policies; this.cdr.markForCheck(); }, error: () => this.snack('No fue posible actualizar el historial.', true) }); }
  private payload(value: ReturnType<PolicyForm['getRawValue']>): CreateTransactionPolicyVersionRequest { return { transactionType: value.transactionType!, prenotificationMode: value.prenotificationMode!, prenotificationLeadBusinessDays: value.prenotificationLeadBusinessDays, effectiveFrom: this.iso(value.effectiveFrom!), effectiveTo: value.effectiveTo ? this.iso(value.effectiveTo) : null, isActive: true, normativeSource: value.normativeSource.trim(), normativeReference: value.normativeReference.trim(), notes: value.notes.trim() || null }; }
  private date(value: string): Date { return new Date(`${value.slice(0, 10)}T00:00:00`); } private iso(value: Date): string { return value.toISOString().slice(0, 10); }
  private snack(message: string, error = false): void { this.snackBar.open(message, 'Cerrar', { duration: 5000, panelClass: error ? 'policy-error' : undefined }); }
  private errorMessage(error: any): string { return error?.error?.message || error?.error?.title || 'No fue posible completar la operación.'; }
}
