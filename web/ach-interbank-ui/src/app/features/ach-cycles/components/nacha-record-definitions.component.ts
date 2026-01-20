import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaRecordDefinitionsService } from '../services/nacha-record-definitions.service';
import { NachaRecordDefinitionDto } from '../models/nacha-record-definition.model';

interface NachaDefinitionRow extends NachaRecordDefinitionDto {
  sourceTypeLabel: string;
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
  private readonly fb = inject(FormBuilder);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  definitions: NachaDefinitionRow[] = [];
  loading = false;
  saving = false;
  editing: NachaRecordDefinitionDto | null = null;

  readonly columns = [
    { key: 'recordCode', label: 'Código', width: '90px' },
    { key: 'sequence', label: 'Orden', width: '90px' },
    { key: 'sourceTypeLabel', label: 'Fuente' },
    { key: 'sourceName', label: 'Origen' },
    { key: 'filterKey', label: 'Filtro' },
    { key: 'isEnabled', label: 'Activo' }
  ];

  readonly sourceTypes = [
    { value: 0, label: 'Custom' },
    { value: 1, label: 'Entity' },
    { value: 2, label: 'View' },
    { value: 3, label: 'Procedure' }
  ];

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
    this.load();
  }

  load(): void {
    this.loading = true;
    this.service
      .getAll()
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (items) => {
          this.definitions = (items ?? []).map((item) => ({
            ...item,
            sourceTypeLabel: this.resolveSourceType(item.sourceType)
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

  private resolveSourceType(value: number): string {
    return this.sourceTypes.find((item) => item.value === value)?.label ?? 'Custom';
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
