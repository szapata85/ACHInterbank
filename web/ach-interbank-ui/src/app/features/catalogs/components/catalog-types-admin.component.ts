import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, Validators } from '@angular/forms';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { CatalogTypesApiService } from '../services/catalog-types-api.service';
import { CatalogTypeItem } from '../models/catalog-type.model';
import { ColDef } from 'ag-grid-community';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { take } from 'rxjs/operators';
import { CatalogActionConfirmDialogComponent, CatalogActionConfirmDialogData } from './catalog-action-confirm-dialog.component';
import { catalogErrorMessage } from './catalog-error-message.util';

@Component({
  selector: 'app-catalog-types-admin',
  standalone: true,
  imports: [SharedModule, MatButtonModule, MatCardModule, MatDialogModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressBarModule],
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
  private readonly dialog = inject(MatDialog);

  catalogType = '';
  title = 'Catálogo';
  subtitle = 'Administra los registros del catálogo.';

  rows: CatalogTypeItem[] = [];
  loading = false;
  loadError = false;
  saving = false;
  deleting = false;
  operationError = '';
  successMessage = '';
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
        editar.setAttribute('aria-label', 'Editar registro ' + String(params.data?._original?.code ?? ''));
        editar.addEventListener('click', () => {
          if (params.data?._original) {
            this.edit(params.data._original);
          }
        });
        const eliminar = document.createElement('button');
        eliminar.type = 'button';
        eliminar.classList.add('link');
        eliminar.innerText = 'Eliminar';
        eliminar.setAttribute('aria-label', 'Eliminar registro ' + String(params.data?._original?.code ?? ''));
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
    code: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(30)]],
    name: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(100)]],
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
    this.operationError = '';
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
    this.operationError = '';
    this.cdr.markForCheck();
  }

  cancelEdit(): void {
    this.editingCode = null;
    this.form.reset({ code: '', name: '', description: '' });
    this.form.get('code')?.enable({ emitEvent: false });
    this.cdr.markForCheck();
  }

  submit(): void {
    if (this.saving || this.deleting || this.loading) return;
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
    this.operationError = '';
    this.successMessage = '';

    const request$ = this.editingCode
      ? this.api.update(this.catalogType, this.editingCode, payload)
      : this.api.create(this.catalogType, payload);

    request$.subscribe({
      next: () => {
        this.successMessage = this.editingCode ? 'Los cambios se guardaron correctamente.' : 'El registro se agregó correctamente.';
        this.notifications.success(this.successMessage);
        this.saving = false;
        this.cancelEdit();
        this.load();
      },
      error: (err) => {
        this.operationError = catalogErrorMessage(err, 'No pudimos guardar el registro. Revisa los datos e inténtalo nuevamente.');
        this.notifications.error(this.operationError);
        this.saving = false;
        this.cdr.markForCheck();
      }
    });
  }

  remove(row: CatalogTypeItem): void {
    if (!this.catalogType || this.saving || this.deleting || this.loading) return;

    this.dialog.open<CatalogActionConfirmDialogComponent, CatalogActionConfirmDialogData, boolean>(
      CatalogActionConfirmDialogComponent,
      {
        width: 'min(92vw, 440px)',
        data: {
          title: 'Eliminar registro',
          message: `¿Quieres eliminar el código ${row.code}? Esta acción no se puede deshacer.`,
          confirmText: 'Eliminar registro',
          destructive: true
        }
      }
    ).afterClosed().pipe(take(1)).subscribe(confirmed => {
      if (!confirmed || this.deleting || this.saving) return;
      this.deleting = true;
      this.operationError = '';
      this.successMessage = '';
      this.cdr.markForCheck();
      this.api.delete(this.catalogType, row.code).subscribe({
        next: () => {
          this.deleting = false;
          this.successMessage = 'El registro se eliminó correctamente.';
          this.notifications.success(this.successMessage);
          if (this.editingCode === row.code) this.cancelEdit();
          this.load();
        },
        error: err => {
          this.deleting = false;
          this.operationError = catalogErrorMessage(err, 'No pudimos eliminar el registro. Inténtalo de nuevo.');
          this.notifications.error(this.operationError);
          this.cdr.markForCheck();
        }
      });
    });
  }
}
