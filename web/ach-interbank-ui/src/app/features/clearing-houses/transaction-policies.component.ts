import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  TemplateRef,
  ViewChild,
  inject
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, finalize, forkJoin, map, of, switchMap, take } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
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
import {
  CloseValidityDialogComponent,
  ConfirmationDialogComponent,
  ConfirmationDialogData
} from './clearing-house-dialogs.component';
import { ClearingHouseContextNavigationComponent } from './clearing-house-context-navigation.component';
import { ClearingHousesService } from './clearing-houses.service';
import { ClearingHouse } from './clearing-houses.models';
import { TransactionPoliciesService } from './transaction-policies.service';
import {
  CreateTransactionPolicyVersionRequest,
  PrenotificationMode,
  TransactionPolicy,
  TransactionPolicyPreview
} from './transaction-policies.models';
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

interface PolicyLoadResult {
  house: ClearingHouse;
  policies: TransactionPolicy[];
}

@Component({
  selector: 'app-transaction-policies',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ClearingHouseContextNavigationComponent,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatDialogModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    MatTableModule,
    MatTooltipModule
  ],
  templateUrl: './transaction-policies.component.html',
  styleUrl: './transaction-policies.component.scss'
})
export class TransactionPoliciesComponent {
  @ViewChild('policyEditorDialog') policyEditorDialog!: TemplateRef<unknown>;

  private readonly route = inject(ActivatedRoute);
  readonly router = inject(Router);
  private readonly houses = inject(ClearingHousesService);
  private readonly policiesApi = inject(TransactionPoliciesService);
  private readonly auth = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private editorRef?: MatDialogRef<unknown>;

  readonly transactionTypes = TransactionTypeEnum;
  readonly displayedColumns = ['transactionType', 'mode', 'lead', 'from', 'to', 'state', 'reference', 'updated', 'actions'];
  readonly canManage = this.auth.hasPermission(['Config.Manage', 'CanManageAch']);
  readonly canRead = this.auth.hasPermission(['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch']);
  readonly canManageCycles = this.auth.hasPermission('ClearingHouses.ManageCycles');
  readonly canManageSpecialDates = this.auth.hasPermission('ClearingHouses.ManageSpecialDates');

  clearingHouse: ClearingHouse | null = null;
  policies: TransactionPolicy[] = [];
  loading = true;
  error = '';
  saving = false;
  metadataPolicy: TransactionPolicy | null = null;
  previewResult: TransactionPolicyPreview | null = null;

  readonly form: PolicyForm = new FormGroup({
    transactionType: new FormControl<TransactionTypeEnum | null>(null, Validators.required),
    prenotificationMode: new FormControl<PrenotificationMode | null>(null, Validators.required),
    prenotificationLeadBusinessDays: new FormControl<number | null>(null, [
      Validators.min(0),
      Validators.max(365),
      integerValidator
    ]),
    effectiveFrom: new FormControl<Date | null>(null, Validators.required),
    effectiveTo: new FormControl<Date | null>(null),
    normativeSource: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(250)] }),
    normativeReference: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(250)] }),
    notes: new FormControl('', { nonNullable: true, validators: Validators.maxLength(2000) })
  }, { validators: [dateRangeValidator] });

  constructor() {
    this.form.controls.prenotificationMode.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(mode => {
        const lead = this.form.controls.prenotificationLeadBusinessDays;
        if (mode === 'Optional' || mode === 'NotApplicable') {
          lead.setValue(null, { emitEvent: false });
          lead.disable({ emitEvent: false });
        } else {
          lead.enable({ emitEvent: false });
        }
      });

    this.route.paramMap.pipe(
      switchMap(params => {
        this.closeEditor();
        this.clearingHouse = null;
        this.policies = [];
        this.previewResult = null;
        this.error = '';
        const id = Number(params.get('id'));
        if (!Number.isInteger(id) || id <= 0) {
          this.loading = false;
          this.error = 'La cámara indicada no es válida.';
          this.cdr.markForCheck();
          return of(null);
        }

        this.loading = true;
        this.cdr.markForCheck();
        return forkJoin({
          house: this.houses.get(id),
          policies: this.policiesApi.list(id)
        }).pipe(
          map(result => result as PolicyLoadResult),
          catchError(error => {
            this.error = this.loadError(error);
            return of(null);
          }),
          finalize(() => {
            this.loading = false;
            this.cdr.markForCheck();
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => {
      if (result) {
        this.clearingHouse = result.house;
        this.policies = result.policies;
      }
      this.cdr.markForCheck();
    });
  }

  get currentDebit(): TransactionPolicy | undefined {
    return this.current(TransactionTypeEnum.Debit);
  }

  get currentCredit(): TransactionPolicy | undefined {
    return this.current(TransactionTypeEnum.Credit);
  }

  createVersion(): void {
    if (!this.canManage) {
      return;
    }
    this.metadataPolicy = null;
    this.previewResult = null;
    this.form.enable({ emitEvent: false });
    this.form.reset({
      transactionType: TransactionTypeEnum.Debit,
      prenotificationMode: 'Mandatory',
      prenotificationLeadBusinessDays: null,
      effectiveFrom: new Date(),
      effectiveTo: null,
      normativeSource: '',
      normativeReference: '',
      notes: ''
    });
    this.form.markAsPristine();
    this.openEditor();
  }

  editMetadata(policy: TransactionPolicy): void {
    if (!this.canManage) {
      return;
    }
    this.metadataPolicy = policy;
    this.previewResult = null;
    this.form.enable({ emitEvent: false });
    this.form.reset({
      transactionType: policy.transactionType,
      prenotificationMode: policy.prenotificationMode,
      prenotificationLeadBusinessDays: policy.prenotificationLeadBusinessDays,
      effectiveFrom: this.date(policy.effectiveFrom),
      effectiveTo: policy.effectiveTo ? this.date(policy.effectiveTo) : null,
      normativeSource: policy.normativeSource,
      normativeReference: policy.normativeReference,
      notes: policy.notes || ''
    });
    this.form.controls.transactionType.disable({ emitEvent: false });
    this.form.controls.prenotificationMode.disable({ emitEvent: false });
    this.form.controls.prenotificationLeadBusinessDays.disable({ emitEvent: false });
    this.form.controls.effectiveFrom.disable({ emitEvent: false });
    this.form.controls.effectiveTo.disable({ emitEvent: false });
    this.form.markAsPristine();
    this.openEditor();
  }

  cancel(): void {
    if (this.form.dirty) {
      this.confirm({
        title: 'Descartar cambios',
        message: 'Los cambios del formulario no se han guardado.',
        confirmText: 'Descartar',
        icon: 'edit_off',
        destructive: true
      }).subscribe(confirmed => {
        if (confirmed) {
          this.closeEditor();
        }
      });
      return;
    }
    this.closeEditor();
  }

  save(): void {
    this.form.markAllAsTouched();
    if (!this.canManage || this.form.invalid || this.saving || !this.clearingHouse) {
      return;
    }
    if (this.metadataPolicy) {
      this.persist();
      return;
    }
    this.confirm({
      title: 'Confirmar nueva versión',
      message: 'La nueva política puede cerrar una vigencia anterior y cambiar los prerrequisitos de exportación NACHA-M.',
      confirmText: 'Crear versión',
      icon: 'warning_amber'
    }).subscribe(confirmed => {
      if (confirmed) {
        this.persist();
      }
    });
  }

  close(policy: TransactionPolicy): void {
    if (!this.clearingHouse || !this.canManage) {
      return;
    }
    this.dialog.open(CloseValidityDialogComponent, {
      data: { transactionType: this.typeLabel(policy.transactionType).toLowerCase(), initialDate: new Date() },
      width: 'min(92vw, 520px)',
      autoFocus: 'dialog',
      restoreFocus: true
    }).afterClosed().pipe(take(1)).subscribe(date => {
      if (!date || !this.clearingHouse) {
        return;
      }
      this.policiesApi.close(this.clearingHouse.id, policy.id, this.iso(date)).subscribe({
        next: () => {
          this.snack('Vigencia cerrada.');
          this.reload();
        },
        error: error => this.snack(this.errorMessage(error), true)
      });
    });
  }

  activate(policy: TransactionPolicy): void {
    if (!this.clearingHouse || !this.canManage) {
      return;
    }
    this.confirm({
      title: 'Activar versión futura',
      message: `Se activará la versión de ${this.typeLabel(policy.transactionType).toLowerCase()} vigente desde ${policy.effectiveFrom}.`,
      confirmText: 'Activar versión',
      icon: 'play_circle'
    }).subscribe(confirmed => {
      if (!confirmed || !this.clearingHouse) {
        return;
      }
      this.policiesApi.activate(this.clearingHouse.id, policy.id).subscribe({
        next: () => {
          this.snack('Versión activada.');
          this.reload();
        },
        error: error => this.snack(this.errorMessage(error), true)
      });
    });
  }

  preview(): void {
    const value = this.form.getRawValue();
    if (!this.clearingHouse || !value.transactionType || !value.effectiveFrom) {
      return;
    }
    this.policiesApi.preview(this.clearingHouse.id, value.transactionType, this.iso(value.effectiveFrom)).subscribe({
      next: result => {
        this.previewResult = result;
        this.cdr.markForCheck();
      },
      error: () => this.snack('No fue posible obtener la vista previa.', true)
    });
  }

  status(policy: TransactionPolicy): string {
    const now = this.iso(new Date());
    if (!policy.isActive) return 'Inactiva';
    if (policy.effectiveFrom > now) return 'Futura';
    if (policy.effectiveTo && policy.effectiveTo < now) return 'Histórica';
    return 'Vigente';
  }

  lead(policy?: TransactionPolicy): string {
    if (!policy || policy.prenotificationMode !== 'Mandatory') {
      return 'No aplica';
    }
    return policy.prenotificationLeadBusinessDays == null
      ? 'Sin plazo mínimo documentado'
      : `${policy.prenotificationLeadBusinessDays} días hábiles`;
  }

  typeLabel(type: TransactionTypeEnum): string {
    return type === TransactionTypeEnum.Debit ? 'Débito' : 'Crédito';
  }

  modeLabel(mode: PrenotificationMode): string {
    return mode === 'Mandatory'
      ? 'Prenotificación obligatoria'
      : mode === 'Optional'
        ? 'Prenotificación opcional'
        : 'No aplica';
  }

  track = (_: number, policy: TransactionPolicy) => policy.id;

  private openEditor(): void {
    this.editorRef = this.dialog.open(this.policyEditorDialog, {
      width: 'min(96vw, 960px)',
      maxHeight: '92vh',
      disableClose: true,
      autoFocus: 'first-tabbable',
      restoreFocus: true
    });
    this.editorRef.afterClosed().pipe(take(1)).subscribe(() => {
      this.metadataPolicy = null;
      this.previewResult = null;
      this.form.enable({ emitEvent: false });
      this.form.markAsPristine();
      this.editorRef = undefined;
      this.cdr.markForCheck();
    });
  }

  private closeEditor(): void {
    this.form.markAsPristine();
    this.editorRef?.close();
  }

  private persist(): void {
    if (!this.clearingHouse) {
      return;
    }
    this.saving = true;
    const value = this.form.getRawValue();
    const request$ = this.metadataPolicy
      ? this.policiesApi.updateMetadata(this.clearingHouse.id, this.metadataPolicy.id, {
          normativeSource: value.normativeSource.trim(),
          normativeReference: value.normativeReference.trim(),
          notes: value.notes.trim() || null
        })
      : this.policiesApi.create(this.clearingHouse.id, this.payload(value));

    request$.pipe(finalize(() => {
      this.saving = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: () => {
        this.snack('La política fue guardada.');
        this.closeEditor();
        this.reload();
      },
      error: error => {
        if (error?.status === 409) {
          this.form.setErrors({ ...this.form.errors, overlap: true });
        }
        this.snack(this.errorMessage(error), true);
      }
    });
  }

  private current(type: TransactionTypeEnum): TransactionPolicy | undefined {
    return this.policies.find(policy => policy.transactionType === type && this.status(policy) === 'Vigente');
  }

  private reload(): void {
    if (!this.clearingHouse) {
      return;
    }
    this.policiesApi.list(this.clearingHouse.id).subscribe({
      next: policies => {
        this.policies = policies;
        this.cdr.markForCheck();
      },
      error: () => this.snack('No fue posible actualizar el historial.', true)
    });
  }

  private payload(value: ReturnType<PolicyForm['getRawValue']>): CreateTransactionPolicyVersionRequest {
    return {
      transactionType: value.transactionType!,
      prenotificationMode: value.prenotificationMode!,
      prenotificationLeadBusinessDays: value.prenotificationLeadBusinessDays,
      effectiveFrom: this.iso(value.effectiveFrom!),
      effectiveTo: value.effectiveTo ? this.iso(value.effectiveTo) : null,
      isActive: true,
      normativeSource: value.normativeSource.trim(),
      normativeReference: value.normativeReference.trim(),
      notes: value.notes.trim() || null
    };
  }

  private confirm(data: ConfirmationDialogData) {
    return this.dialog.open(ConfirmationDialogComponent, {
      data,
      width: 'min(92vw, 540px)',
      autoFocus: 'dialog',
      restoreFocus: true
    }).afterClosed().pipe(take(1));
  }

  private date(value: string): Date {
    return new Date(`${value.slice(0, 10)}T00:00:00`);
  }

  private iso(value: Date): string {
    const local = new Date(value.getTime() - value.getTimezoneOffset() * 60_000);
    return local.toISOString().slice(0, 10);
  }

  private snack(message: string, error = false): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: 5000,
      panelClass: error ? 'policy-error' : undefined
    });
  }

  private loadError(error: { status?: number }): string {
    if (error?.status === 404) return 'La cámara solicitada no existe o ya no está disponible.';
    if (error?.status === 401) return 'Su sesión expiró. Inicie sesión nuevamente.';
    if (error?.status === 403) return 'No tiene permisos para consultar estas políticas.';
    return 'No fue posible cargar las políticas transaccionales.';
  }

  private errorMessage(error: any): string {
    return error?.error?.detail
      || error?.error?.message
      || error?.error?.title
      || 'No fue posible completar la operación.';
  }
}

function integerValidator(control: AbstractControl<number | null>): ValidationErrors | null {
  return control.value == null || Number.isInteger(Number(control.value)) ? null : { integer: true };
}

function dateRangeValidator(control: AbstractControl): ValidationErrors | null {
  const from = control.get('effectiveFrom')?.value as Date | null;
  const to = control.get('effectiveTo')?.value as Date | null;
  return from && to && to < from ? { dateRange: true } : null;
}
