import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaRecordDefinitionsService } from '../services/nacha-record-definitions.service';
import { NachaRecordDefinitionDto } from '../models/nacha-record-definition.model';
import { NachaLayoutsService } from '../services/nacha-layouts.service';
import { NachaRecordLayoutDto } from '../models/nacha-layout.model';

interface NachaDefinitionRow extends NachaRecordDefinitionDto {
  sourceTypeLabel: string;
  sourceDisplay: string;
  layoutSummary?: string;
}

@Component({
  selector: 'app-nacha-record-definitions',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './nacha-record-definitions.component.html',
  styleUrls: ['./nacha-record-definitions.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaRecordDefinitionsComponent implements OnInit {
  private readonly service = inject(NachaRecordDefinitionsService);
  private readonly layoutsService = inject(NachaLayoutsService);
  private readonly fb = inject(FormBuilder);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  definitions: NachaDefinitionRow[] = [];
  layoutsByCode: Record<string, NachaRecordLayoutDto> = {};
  loading = false;
  saving = false;
  editing: NachaRecordDefinitionDto | null = null;

  readonly columns = [
    { key: 'recordCode', label: 'Código', width: '90px' },
    { key: 'sequence', label: 'Orden', width: '90px' },
    { key: 'sourceTypeLabel', label: 'Fuente' },
    { key: 'sourceDisplay', label: 'Origen' },
    { key: 'filterKey', label: 'Filtro' },
    { key: 'layoutSummary', label: 'Layout (campos)' },
    { key: 'isEnabled', label: 'Activo' }
  ];

  readonly sourceTypes = [
    { value: 0, label: 'Custom' },
    { value: 1, label: 'Entity' },
    { value: 2, label: 'View' },
    { value: 3, label: 'Procedure' }
  ];

  readonly sourceOptionsByType: Record<number, Array<{ value: string; label: string }>> = {
    0: [],
    1: [
      { value: 'AchBatch', label: 'AchBatch (tabla: AchBatches)' },
      { value: 'AchTransaction', label: 'AchTransaction (tabla: AchTransactions)' },
      { value: 'AchTransactionAddenda', label: 'AchTransactionAddenda (tabla: AchTransactionAddendas)' }
    ],
    2: [],
    3: []
  };

  form = this.fb.nonNullable.group({
    id: [0],
    recordCode: ['', [Validators.required, Validators.maxLength(5)]],
    sequence: [10, [Validators.required, Validators.min(1)]],
    sourceType: [0, Validators.required],
    sourceName: [''],
    filterKey: [''],
    isEnabled: [true]
  });

  ngOnInit(): void {
    this.form.controls.sourceType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((sourceType) => {
        if (sourceType === 0) {
          this.form.patchValue({ sourceName: '' }, { emitEvent: false });
        }
        this.cdr.markForCheck();
      });

    this.load();
  }

  get sourceNameOptions(): Array<{ value: string; label: string }> {
    return this.sourceOptionsByType[this.form.getRawValue().sourceType] ?? [];
  }

  get showSourceNameSelect(): boolean {
    const sourceType = this.form.getRawValue().sourceType;
    return sourceType !== 0 && this.sourceNameOptions.length > 0;
  }

  get sourceTypeLabel(): string {
    return this.resolveSourceType(this.form.getRawValue().sourceType);
  }

  load(): void {
    this.loading = true;
    forkJoin({
      definitions: this.service.getAll(),
      layouts: this.layoutsService.getAll()
    })
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: ({ definitions, layouts }) => {
          this.layoutsByCode = (layouts ?? []).reduce<Record<string, NachaRecordLayoutDto>>((acc, layout) => {
            acc[layout.recordCode] = layout;
            return acc;
          }, {});

          this.definitions = (definitions ?? []).map((item) => ({
            ...item,
            sourceTypeLabel: this.resolveSourceType(item.sourceType),
            sourceDisplay: this.resolveSourceDisplay(item),
            layoutSummary: this.resolveLayoutSummary(item.recordCode)
          }));
          this.cdr.markForCheck();
        },
        error: () => {
          this.notifications.error('No fue posible cargar las definiciones NACHA');
        }
      });
  }

  startCreate(): void {
    this.editing = null;
    this.form.reset({
      id: 0,
      recordCode: '',
      sequence: 10,
      sourceType: 0,
      sourceName: '',
      filterKey: '',
      isEnabled: true
    });
    this.cdr.markForCheck();
  }

  startEdit(definition: NachaRecordDefinitionDto): void {
    this.editing = definition;
    this.form.reset({
      id: definition.id,
      recordCode: definition.recordCode,
      sequence: definition.sequence,
      sourceType: definition.sourceType,
      sourceName: definition.sourceName ?? '',
      filterKey: definition.filterKey ?? '',
      isEnabled: definition.isEnabled
    });
    this.cdr.markForCheck();
  }

  cancel(): void {
    this.editing = null;
    this.form.reset();
    this.cdr.markForCheck();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.toPayload();
    this.saving = true;

    const request$ = payload.id
      ? this.service.update(payload)
      : this.service.create(payload);

    request$
      .pipe(finalize(() => {
        this.saving = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.notifications.success('Definición guardada correctamente');
          this.cancel();
          this.load();
        },
        error: () => {
          this.notifications.error('No fue posible guardar la definición');
        }
      });
  }

  remove(definition: NachaRecordDefinitionDto): void {
    if (!confirm(`¿Eliminar definición ${definition.recordCode}?`)) {
      return;
    }

    this.service.delete(definition.id).subscribe({
      next: () => {
        this.notifications.success('Definición eliminada');
        this.load();
      },
      error: () => {
        this.notifications.error('No fue posible eliminar la definición');
      }
    });
  }

  manageLayout(): void {
    const recordCode = this.form.getRawValue().recordCode?.trim();
    void this.router.navigate(['/ach-cycles/nacha/layouts'], {
      queryParams: recordCode ? { recordCode } : undefined
    });
  }

  private resolveSourceType(value: number): string {
    return this.sourceTypes.find((item) => item.value === value)?.label ?? 'Custom';
  }

  private resolveLayoutSummary(recordCode: string): string {
    const layout = this.layoutsByCode[recordCode];
    if (!layout) {
      return 'Sin layout';
    }

    return `${layout.recordType} (${layout.fields?.length ?? 0})`;
  }

  private resolveSourceDisplay(definition: NachaRecordDefinitionDto): string {
    if (definition.sourceType === 0) {
      return 'Calculado (Custom)';
    }

    return definition.sourceName?.trim() || '-';
  }

  private toPayload(): NachaRecordDefinitionDto {
    const raw = this.form.getRawValue();
    return {
      id: raw.id,
      recordCode: raw.recordCode,
      sequence: raw.sequence,
      sourceType: raw.sourceType,
      sourceName: raw.sourceName || null,
      filterKey: raw.filterKey || null,
      isEnabled: raw.isEnabled
    };
  }
}
