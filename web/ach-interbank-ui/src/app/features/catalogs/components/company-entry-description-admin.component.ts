import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { CompanyEntryDescriptionItem } from '../models/company-entry-description.model';
import { CompanyEntryDescriptionsApiService } from '../services/company-entry-descriptions-api.service';
import { ColDef } from 'ag-grid-community';

@Component({
  selector: 'app-company-entry-description-admin',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './company-entry-description-admin.component.html',
  styleUrls: ['./company-entry-description-admin.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CompanyEntryDescriptionAdminComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CompanyEntryDescriptionsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  rows: CompanyEntryDescriptionItem[] = [];
  loading = false;
  saving = false;
  editingId: number | null = null;

  readonly columnas: ColDef[] = [
    { field: 'id', headerName: 'Id', sortable: true, filter: 'agNumberColumnFilter', maxWidth: 110 },
    { field: 'term', headerName: 'Término', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'description', headerName: 'Descripción', sortable: true, filter: 'agTextColumnFilter', flex: 1 },
    { field: 'sec', headerName: 'SEC', sortable: true, filter: 'agTextColumnFilter', maxWidth: 120 },
    { field: 'estado', headerName: 'Estado', sortable: true, filter: 'agTextColumnFilter', maxWidth: 120 },
    {
      field: 'acciones',
      headerName: 'Acciones',
      sortable: false,
      filter: false,
      maxWidth: 150,
      cellRenderer: (params: any) => {
        const container = document.createElement('div');
        const editar = document.createElement('button');
        editar.type = 'button';
        editar.classList.add('link');
        editar.innerText = 'Editar';
        editar.addEventListener('click', () => this.edit(params.data._original));
        const eliminar = document.createElement('button');
        eliminar.type = 'button';
        eliminar.classList.add('link');
        eliminar.innerText = 'Eliminar';
        eliminar.addEventListener('click', () => this.remove(params.data._original));
        container.append(editar, eliminar);
        return container;
      }
    }
  ];

  get filasGrilla(): any[] {
    return this.rows.map((row) => ({
      id: row.id,
      term: row.term,
      description: row.description,
      sec: row.standardEntryClassCode,
      estado: row.isActive ? 'Activo' : 'Inactivo',
      acciones: 'Editar/Eliminar',
      _original: row
    }));
  }

  readonly form = this.fb.group({
    term: ['', [Validators.required, Validators.maxLength(12)]],
    description: ['', [Validators.required, Validators.maxLength(255)]],
    standardEntryClassCode: ['PPD' as 'PPD' | 'CCD', [Validators.required]],
    isActive: [true, [Validators.required]]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.api.list().subscribe({
      next: (rows) => {
        this.rows = rows ?? [];
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No fue posible cargar el catálogo.');
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  edit(row: CompanyEntryDescriptionItem): void {
    this.editingId = row.id;
    this.form.patchValue({
      term: row.term,
      description: row.description,
      standardEntryClassCode: row.standardEntryClassCode,
      isActive: row.isActive
    });
  }

  cancelEdit(): void {
    this.editingId = null;
    this.form.reset({ term: '', description: '', standardEntryClassCode: 'PPD', isActive: true });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = {
      term: String(this.form.getRawValue().term ?? '').trim().toUpperCase(),
      description: String(this.form.getRawValue().description ?? '').trim(),
      standardEntryClassCode: String(this.form.getRawValue().standardEntryClassCode ?? 'PPD').trim().toUpperCase() as 'PPD' | 'CCD',
      isActive: Boolean(this.form.getRawValue().isActive)
    };

    this.saving = true;

    const request$ = this.editingId
      ? this.api.update(this.editingId, payload)
      : this.api.create(payload);

    request$.subscribe({
      next: () => {
        this.notifications.success(this.editingId ? 'Registro actualizado.' : 'Registro creado.');
        this.saving = false;
        this.cancelEdit();
        this.load();
      },
      error: (err) => {
        const message = err?.error?.message || err?.error || 'No fue posible guardar el registro.';
        this.notifications.error(String(message));
        this.saving = false;
        this.cdr.markForCheck();
      }
    });
  }

  remove(row: CompanyEntryDescriptionItem): void {
    const ok = window.confirm(`¿Deseas eliminar el término ${row.term}?`);
    if (!ok) {
      return;
    }

    this.api.delete(row.id).subscribe({
      next: () => {
        this.notifications.success('Registro eliminado.');
        this.load();
      },
      error: (err) => {
        const message = err?.error?.message || err?.error || 'No fue posible eliminar el registro.';
        this.notifications.error(String(message));
      }
    });
  }
}
