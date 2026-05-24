import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, Validators } from '@angular/forms';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { CatalogTypesApiService } from '../services/catalog-types-api.service';
import { CatalogTypeItem } from '../models/catalog-type.model';
import { ColDef } from 'ag-grid-community';

@Component({
  selector: 'app-catalog-types-admin',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './catalog-types-admin.component.html',
  styleUrls: ['./catalog-types-admin.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CatalogTypesAdminComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CatalogTypesApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  catalogType = '';
  title = 'Catálogo';
  subtitle = 'Administra los registros del catálogo.';

  rows: CatalogTypeItem[] = [];
  loading = false;
  loadError = false;
  saving = false;
  editingCode: string | null = null;

  readonly columnas: ColDef[] = [
    { field: 'code', headerName: 'Código', sortable: true, filter: 'agTextColumnFilter', minWidth: 120, maxWidth: 160 },
    { field: 'name', headerName: 'Nombre', sortable: true, filter: 'agTextColumnFilter', minWidth: 180 },
    { field: 'description', headerName: 'Descripción', sortable: true, filter: 'agTextColumnFilter', flex: 1, minWidth: 240 },
    {
      field: 'acciones',
      headerName: 'Acciones',
      sortable: false,
      filter: false,
      floatingFilter: false,
      minWidth: 160,
      maxWidth: 180,
      cellRenderer: (params: any) => {
        const container = document.createElement('div');
        container.classList.add('row-actions');
        const editar = document.createElement('button');
        editar.type = 'button';
        editar.classList.add('link');
        editar.innerText = 'Editar';
        editar.addEventListener('click', () => {
          if (params.data?._original) {
            this.edit(params.data._original);
          }
        });
        const eliminar = document.createElement('button');
        eliminar.type = 'button';
        eliminar.classList.add('link');
        eliminar.innerText = 'Eliminar';
        eliminar.addEventListener('click', () => {
          if (params.data?._original) {
            this.remove(params.data._original);
          }
        });
        container.append(editar, eliminar);
        return container;
      }
    }
  ];

  get filasGrilla(): any[] {
    return this.rows.map((row) => ({
      code: row.code,
      name: row.name,
      description: row.description || '-',
      acciones: 'Editar/Eliminar',
      _original: row
    }));
  }

  readonly form = this.fb.group({
    code: ['', [Validators.required, Validators.maxLength(30)]],
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(200)]]
  });

  ngOnInit(): void {
    const data = this.route.snapshot.data;
    this.catalogType = String(data['catalogType'] ?? '').trim();
    this.title = String(data['title'] ?? this.title);
    this.subtitle = String(data['subtitle'] ?? this.subtitle);
    this.load();
  }

  load(): void {
    if (!this.catalogType) {
      this.notifications.error('No se pudo determinar el tipo de catálogo.');
      return;
    }

    this.loading = true;
    this.loadError = false;
    this.api.list(this.catalogType).subscribe({
      next: (rows) => {
        this.rows = rows ?? [];
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No fue posible cargar el catálogo.');
        this.rows = [];
        this.loadError = true;
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  edit(row: CatalogTypeItem): void {
    this.editingCode = row.code;
    this.form.patchValue({
      code: row.code,
      name: row.name,
      description: row.description ?? ''
    });
    this.form.get('code')?.disable({ emitEvent: false });
  }

  cancelEdit(): void {
    this.editingCode = null;
    this.form.reset({ code: '', name: '', description: '' });
    this.form.get('code')?.enable({ emitEvent: false });
  }

  submit(): void {
    if (this.form.invalid || !this.catalogType) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = {
      code: String(this.form.getRawValue().code ?? '').trim().toUpperCase(),
      name: String(this.form.getRawValue().name ?? '').trim(),
      description: String(this.form.getRawValue().description ?? '').trim() || null
    };

    this.saving = true;

    const request$ = this.editingCode
      ? this.api.update(this.catalogType, this.editingCode, payload)
      : this.api.create(this.catalogType, payload);

    request$.subscribe({
      next: () => {
        this.notifications.success(this.editingCode ? 'Registro actualizado.' : 'Registro creado.');
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

  remove(row: CatalogTypeItem): void {
    if (!this.catalogType) {
      return;
    }

    const ok = window.confirm(`¿Deseas eliminar el código ${row.code}?`);
    if (!ok) {
      return;
    }

    this.api.delete(this.catalogType, row.code).subscribe({
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
