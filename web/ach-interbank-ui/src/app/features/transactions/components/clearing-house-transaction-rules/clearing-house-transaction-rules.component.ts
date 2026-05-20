import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import { OpcionSelectorBuscable } from '../../../../shared/components/ui/ui-selector-buscable.component';
import { ClearingHousesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import { TransactionTypeEnum } from '../../transactions.types';
import {
  ClearingHouseTransactionRuleItem,
  PrenotificationRequirementMode,
  SaveClearingHouseTransactionRuleRequest,
  TransactionNature,
  TransactionPrerequisitePreviewResponse,
  ValidationRequirementMode
} from '../../transactions.models';
import { ClearingHouseTransactionRulesApiService } from '../../services/clearing-house-transaction-rules-api.service';

type RuleNatureFilter = TransactionNature | 'all';

@Component({
  selector: 'app-clearing-house-transaction-rules',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './clearing-house-transaction-rules.component.html',
  styleUrls: ['./clearing-house-transaction-rules.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClearingHouseTransactionRulesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly rulesApi = inject(ClearingHouseTransactionRulesApiService);
  private readonly clearingHousesApi = inject(ClearingHousesApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;
  saving = false;
  showForm = false;
  previewLoading = false;
  loadError: string | null = null;
  editingRule: ClearingHouseTransactionRuleItem | null = null;
  previewResult: TransactionPrerequisitePreviewResponse | null = null;
  rules: ClearingHouseTransactionRuleItem[] = [];
  clearingHouses: Array<{ id: number; name: string }> = [];

  readonly transactionTypes = TransactionTypeEnum;
  readonly natureOptions: Array<{ value: RuleNatureFilter; label: string }> = [
    { value: 'all', label: 'Todas' },
    { value: 'Credit', label: 'Credito' },
    { value: 'Debit', label: 'Debito' }
  ];
  readonly formNatureOptions: Array<{ value: TransactionNature; label: string }> = [
    { value: 'Credit', label: 'Credito' },
    { value: 'Debit', label: 'Debito' }
  ];
  readonly modeOptions: Array<{ value: PrenotificationRequirementMode; label: string }> = [
    { value: 'Mandatory', label: 'Obligatoria' },
    { value: 'Optional', label: 'Opcional' },
    { value: 'NotApplicable', label: 'No aplica' }
  ];
  readonly validationModeOptions: Array<{ value: ValidationRequirementMode; label: string }> = [
    { value: 'Mandatory', label: 'Obligatoria' },
    { value: 'Optional', label: 'Opcional' },
    { value: 'NotApplicable', label: 'No aplica' }
  ];

  readonly columnDefs: ColDef<ClearingHouseTransactionRuleItem>[] = [
    { headerName: 'Camara', minWidth: 190, valueGetter: (params) => params.data?.clearingHouseName || params.data?.clearingHouseId },
    { headerName: 'Naturaleza', width: 130, valueGetter: (params) => this.natureLabel(params.data?.transactionNature) },
    { headerName: 'Tipo', width: 110, valueGetter: (params) => this.transactionTypeLabel(params.data?.transactionType) },
    { headerName: 'Prenotificacion', width: 150, valueGetter: (params) => this.modeLabel(params.data?.prenotificationMode) },
    { headerName: 'Validacion ID', width: 140, valueGetter: (params) => this.modeLabel(params.data?.receiverIdentificationValidationMode) },
    {
      headerName: 'Vigencia',
      minWidth: 210,
      valueGetter: (params) => {
        const from = this.toDateText(params.data?.effectiveFrom);
        const to = params.data?.effectiveTo ? this.toDateText(params.data.effectiveTo) : 'abierta';
        return `${from} a ${to}`;
      }
    },
    { headerName: 'Fuente', minWidth: 190, field: 'normativeSource' },
    { headerName: 'Estado', width: 110, valueGetter: (params) => params.data?.isActive ? 'Activa' : 'Inactiva' },
    {
      headerName: 'Acciones',
      width: 170,
      sortable: false,
      filter: false,
      cellRenderer: (params) => params.data?.isActive
        ? '<button class="btn btn-outline btn-grid" data-action="edit" title="Editar"><span class="material-symbols-outlined">edit</span></button> <button class="btn btn-danger btn-grid" data-action="deactivate" title="Inactivar"><span class="material-symbols-outlined">block</span></button>'
        : '<button class="btn btn-outline btn-grid" data-action="edit" title="Editar"><span class="material-symbols-outlined">edit</span></button> <button class="btn btn-outline btn-grid" data-action="activate" title="Activar"><span class="material-symbols-outlined">check_circle</span></button>',
      onCellClicked: (params) => {
        const target = params.event?.target as HTMLElement | null;
        const action = target?.getAttribute('data-action');

        if (action === 'edit' && params.data) {
          this.edit(params.data);
        } else if (action === 'activate' && params.data) {
          this.setActive(params.data, true);
        } else if (action === 'deactivate' && params.data) {
          this.setActive(params.data, false);
        }
      }
    }
  ];

  readonly filterForm = this.fb.group({
    clearingHouseId: [null as number | null],
    transactionNature: ['all' as RuleNatureFilter],
    includeInactive: [false]
  });

  readonly form = this.fb.group({
    clearingHouseId: [null as number | null, Validators.required],
    transactionNature: ['Debit' as TransactionNature, Validators.required],
    transactionType: [TransactionTypeEnum.Debit, Validators.required],
    requiresPrenotification: [true],
    prenotificationMode: ['Mandatory' as PrenotificationRequirementMode, Validators.required],
    requiresReceiverIdentificationValidation: [true],
    receiverIdentificationValidationMode: ['Mandatory' as ValidationRequirementMode, Validators.required],
    appliesToNachaExport: [true],
    appliesToMonetaryTransactions: [true],
    effectiveFrom: [this.todayInputValue(), Validators.required],
    effectiveTo: [null as string | null],
    normativeSource: ['', [Validators.required, Validators.maxLength(160)]],
    normativeReference: ['', [Validators.required, Validators.maxLength(220)]],
    notes: ['', Validators.maxLength(600)]
  });

  ngOnInit(): void {
    this.loadClearingHouses();
    this.form.controls.prenotificationMode.valueChanges.subscribe(() => this.syncModeFlags());
    this.form.controls.receiverIdentificationValidationMode.valueChanges.subscribe(() => this.syncModeFlags());
    this.search();
  }

  get clearingHouseOptions(): OpcionSelectorBuscable[] {
    return this.clearingHouses.map((house) => ({ valor: house.id, etiqueta: house.name }));
  }

  get natureSelectorOptions(): OpcionSelectorBuscable[] {
    return this.natureOptions.map((option) => ({ valor: option.value, etiqueta: option.label }));
  }

  get formNatureSelectorOptions(): OpcionSelectorBuscable[] {
    return this.formNatureOptions.map((option) => ({ valor: option.value, etiqueta: option.label }));
  }

  get modeSelectorOptions(): OpcionSelectorBuscable[] {
    return this.modeOptions.map((option) => ({ valor: option.value, etiqueta: option.label }));
  }

  get validationModeSelectorOptions(): OpcionSelectorBuscable[] {
    return this.validationModeOptions.map((option) => ({ valor: option.value, etiqueta: option.label }));
  }

  search(): void {
    this.loading = true;
    this.loadError = null;
    this.cdr.markForCheck();

    this.rulesApi
      .getRules({
        clearingHouseId: this.filterForm.controls.clearingHouseId.value,
        transactionNature: this.filterForm.controls.transactionNature.value ?? 'all',
        includeInactive: this.filterForm.controls.includeInactive.value ?? false
      })
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (rules) => {
          this.rules = rules;
          this.cdr.markForCheck();
        },
        error: () => {
          this.rules = [];
          this.loadError = 'No fue posible consultar reglas por camara.';
          this.notifications.error(this.loadError);
          this.cdr.markForCheck();
        }
      });
  }

  openCreateForm(): void {
    this.showForm = true;
    this.editingRule = null;
    this.previewResult = null;
    this.form.reset({
      clearingHouseId: this.filterForm.controls.clearingHouseId.value,
      transactionNature: 'Debit',
      transactionType: TransactionTypeEnum.Debit,
      requiresPrenotification: true,
      prenotificationMode: 'Mandatory',
      requiresReceiverIdentificationValidation: true,
      receiverIdentificationValidationMode: 'Mandatory',
      appliesToNachaExport: true,
      appliesToMonetaryTransactions: true,
      effectiveFrom: this.todayInputValue(),
      effectiveTo: null,
      normativeSource: '',
      normativeReference: '',
      notes: ''
    });
  }

  edit(rule: ClearingHouseTransactionRuleItem): void {
    this.showForm = true;
    this.editingRule = rule;
    this.previewResult = null;
    this.form.reset({
      clearingHouseId: rule.clearingHouseId,
      transactionNature: rule.transactionNature,
      transactionType: rule.transactionType,
      requiresPrenotification: rule.requiresPrenotification,
      prenotificationMode: rule.prenotificationMode,
      requiresReceiverIdentificationValidation: rule.requiresReceiverIdentificationValidation,
      receiverIdentificationValidationMode: rule.receiverIdentificationValidationMode,
      appliesToNachaExport: rule.appliesToNachaExport,
      appliesToMonetaryTransactions: rule.appliesToMonetaryTransactions,
      effectiveFrom: this.toInputDate(rule.effectiveFrom),
      effectiveTo: rule.effectiveTo ? this.toInputDate(rule.effectiveTo) : null,
      normativeSource: rule.normativeSource,
      normativeReference: rule.normativeReference,
      notes: rule.notes
    });
  }

  closeForm(): void {
    this.showForm = false;
    this.editingRule = null;
    this.previewResult = null;
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.saving) {
      this.notifications.warning('Revise los campos obligatorios.');
      return;
    }

    const payload = this.toPayload();
    const request$ = this.editingRule
      ? this.rulesApi.update(this.editingRule.id, payload)
      : this.rulesApi.create(payload);

    this.saving = true;
    this.cdr.markForCheck();

    request$
      .pipe(finalize(() => {
        this.saving = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.notifications.success('Regla guardada correctamente.');
          this.closeForm();
          this.search();
        },
        error: (error: HttpErrorResponse) => {
          const message = error.error?.message ?? 'No fue posible guardar la regla.';
          this.notifications.error(message);
        }
      });
  }

  preview(): void {
    this.form.markAllAsTouched();
    if (!this.form.controls.clearingHouseId.value || !this.form.controls.effectiveFrom.value) {
      return;
    }

    this.previewLoading = true;
    this.previewResult = null;
    this.cdr.markForCheck();

    this.rulesApi
      .preview({
        clearingHouseId: Number(this.form.controls.clearingHouseId.value),
        transactionType: Number(this.form.controls.transactionType.value) as TransactionTypeEnum,
        effectiveEntryDate: this.form.controls.effectiveFrom.value,
        appliesToNachaExport: this.form.controls.appliesToNachaExport.value ?? true
      })
      .pipe(finalize(() => {
        this.previewLoading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (result) => {
          this.previewResult = result;
          this.cdr.markForCheck();
        },
        error: () => this.notifications.error('No fue posible ejecutar la vista previa.')
      });
  }

  setActive(rule: ClearingHouseTransactionRuleItem, active: boolean): void {
    const request$ = active ? this.rulesApi.activate(rule.id) : this.rulesApi.deactivate(rule.id);
    request$.subscribe({
      next: () => {
        this.notifications.success(active ? 'Regla activada.' : 'Regla inactivada.');
        this.search();
      },
      error: () => this.notifications.error('No fue posible actualizar el estado de la regla.')
    });
  }

  syncModeFlags(): void {
    const mode = this.form.controls.prenotificationMode.value;
    this.form.controls.requiresPrenotification.setValue(mode === 'Mandatory', { emitEvent: false });

    const validationMode = this.form.controls.receiverIdentificationValidationMode.value;
    this.form.controls.requiresReceiverIdentificationValidation.setValue(validationMode === 'Mandatory', { emitEvent: false });
  }

  natureLabel(value?: TransactionNature): string {
    return value === 'Debit' ? 'Debito' : value === 'Credit' ? 'Credito' : '';
  }

  transactionTypeLabel(value?: TransactionTypeEnum): string {
    return value === TransactionTypeEnum.Debit ? 'Debito' : value === TransactionTypeEnum.Credit ? 'Credito' : `${value ?? ''}`;
  }

  modeLabel(value?: PrenotificationRequirementMode | ValidationRequirementMode): string {
    return this.modeOptions.find((option) => option.value === value)?.label
      ?? this.validationModeOptions.find((option) => option.value === value)?.label
      ?? '';
  }

  toDateText(value?: string): string {
    return value ? this.toInputDate(value) : '';
  }

  private toPayload(): SaveClearingHouseTransactionRuleRequest {
    return {
      clearingHouseId: Number(this.form.controls.clearingHouseId.value),
      transactionNature: this.form.controls.transactionNature.value ?? 'Debit',
      transactionType: Number(this.form.controls.transactionType.value) as TransactionTypeEnum,
      requiresPrenotification: this.form.controls.requiresPrenotification.value ?? false,
      prenotificationMode: this.form.controls.prenotificationMode.value ?? 'Optional',
      requiresReceiverIdentificationValidation: this.form.controls.requiresReceiverIdentificationValidation.value ?? false,
      receiverIdentificationValidationMode: this.form.controls.receiverIdentificationValidationMode.value ?? 'Optional',
      appliesToNachaExport: this.form.controls.appliesToNachaExport.value ?? true,
      appliesToMonetaryTransactions: this.form.controls.appliesToMonetaryTransactions.value ?? true,
      effectiveFrom: this.form.controls.effectiveFrom.value ?? this.todayInputValue(),
      effectiveTo: this.form.controls.effectiveTo.value || null,
      normativeSource: (this.form.controls.normativeSource.value ?? '').trim(),
      normativeReference: (this.form.controls.normativeReference.value ?? '').trim(),
      notes: (this.form.controls.notes.value ?? '').trim()
    };
  }

  private loadClearingHouses(): void {
    this.clearingHousesApi.list().subscribe({
      next: (houses) => {
        this.clearingHouses = houses.map((house) => ({ id: house.id, name: house.name }));
        this.cdr.markForCheck();
      },
      error: () => this.notifications.error('No fue posible cargar camaras de compensacion.')
    });
  }

  private todayInputValue(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private toInputDate(value: string): string {
    return value.slice(0, 10);
  }
}
