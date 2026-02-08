import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormArray, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { NachaRecordFieldDto, NachaRecordLayoutDto } from '../models/nacha-layout.model';
import { NachaLayoutsService } from '../services/nacha-layouts.service';
import { NotificationService } from '../../../core/services/notification.service';

interface NachaLayoutRow extends NachaRecordLayoutDto {
  fieldsCount: number;
}

@Component({
  selector: 'app-nacha-layouts',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './nacha-layouts.component.html',
  styleUrls: ['./nacha-layouts.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaLayoutsComponent implements OnInit {
  private readonly service = inject(NachaLayoutsService);
  private readonly fb = inject(FormBuilder);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly route = inject(ActivatedRoute);

  layouts: NachaLayoutRow[] = [];
  loading = false;
  saving = false;
  editing: NachaRecordLayoutDto | null = null;

  readonly columns = [
    { key: 'recordCode', label: 'Código', width: '90px' },
    { key: 'recordType', label: 'Tipo' },
    { key: 'totalLength', label: 'Longitud' },
    { key: 'fieldsCount', label: 'Campos' }
  ];

  form = this.fb.nonNullable.group({
    id: [0],
    recordType: ['', [Validators.required, Validators.maxLength(100)]],
    recordCode: ['', [Validators.required, Validators.maxLength(10)]],
    totalLength: [106, [Validators.required, Validators.min(1)]],
    description: [''],
    fields: this.fb.array([])
  });

  get fields(): FormArray {
    return this.form.get('fields') as FormArray;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const targetRecordCode = this.route.snapshot.queryParamMap.get('recordCode')?.trim();
    this.loading = true;
    this.service
      .getAll()
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (items) => {
          this.layouts = (items ?? []).map((layout) => ({
            ...layout,
            fieldsCount: layout.fields?.length ?? 0
          }));

          if (targetRecordCode) {
            const selected = this.layouts.find((layout) => layout.recordCode === targetRecordCode);
            if (selected) {
              this.startEdit(selected);
            }
          }

          this.cdr.markForCheck();
        },
        error: () => {
          this.notifications.error('No fue posible cargar los layouts NACHA');
        }
      });
  }

  startCreate(): void {
    this.editing = null;
    this.form.reset({
      id: 0,
      recordType: '',
      recordCode: '',
      totalLength: 106,
      description: ''
    });
    this.fields.clear();
    this.addField();
    this.cdr.markForCheck();
  }

  startEdit(layout: NachaRecordLayoutDto): void {
    this.editing = layout;
    this.form.reset({
      id: layout.id,
      recordType: layout.recordType,
      recordCode: layout.recordCode,
      totalLength: layout.totalLength,
      description: layout.description ?? ''
    });
    this.fields.clear();
    (layout.fields ?? []).forEach((field) => this.addField(field));
    if (this.fields.length === 0) {
      this.addField();
    }
    this.cdr.markForCheck();
  }

  addField(field?: NachaRecordFieldDto): void {
    this.fields.push(this.fb.group({
      id: [field?.id ?? 0],
      fieldName: [field?.fieldName ?? '', Validators.required],
      startPosition: [field?.startPosition ?? 1, Validators.required],
      length: [field?.length ?? 1, Validators.required],
      padChar: [field?.padChar ?? ' '],
      justification: [field?.justification ?? 'L'],
      dbColumn: [field?.dbColumn ?? '', Validators.required],
      format: [field?.format ?? '']
    }));
  }

  removeField(index: number): void {
    this.fields.removeAt(index);
  }

  cancel(): void {
    this.editing = null;
    this.form.reset();
    this.fields.clear();
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
          this.notifications.success('Layout guardado correctamente');
          this.cancel();
          this.load();
        },
        error: () => {
          this.notifications.error('No fue posible guardar el layout');
        }
      });
  }

  remove(layout: NachaRecordLayoutDto): void {
    if (!confirm(`¿Eliminar layout ${layout.recordCode}?`)) {
      return;
    }

    this.service.delete(layout.id).subscribe({
      next: () => {
        this.notifications.success('Layout eliminado');
        this.load();
      },
      error: () => {
        this.notifications.error('No fue posible eliminar el layout');
      }
    });
  }

  private toPayload(): NachaRecordLayoutDto {
    const raw = this.form.getRawValue();
    return {
      id: raw.id,
      recordType: raw.recordType,
      recordCode: raw.recordCode,
      totalLength: raw.totalLength,
      description: raw.description || null,
      fields: (raw.fields as NachaRecordFieldDto[] ?? []).map((field) => ({
        id: field.id,
        fieldName: field.fieldName,
        startPosition: field.startPosition,
        length: field.length,
        padChar: field.padChar,
        justification: field.justification,
        dbColumn: field.dbColumn,
        format: field.format || null
      }))
    };
  }
}
