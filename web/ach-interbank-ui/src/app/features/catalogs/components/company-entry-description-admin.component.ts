import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { CompanyEntryDescriptionItem } from '../models/company-entry-description.model';
import { CompanyEntryDescriptionsApiService } from '../services/company-entry-descriptions-api.service';
import { ColDef } from 'ag-grid-community';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { take } from 'rxjs/operators';
import { CatalogActionConfirmDialogComponent, CatalogActionConfirmDialogData } from './catalog-action-confirm-dialog.component';
import { catalogErrorMessage } from './catalog-error-message.util';

@Component({
  selector: 'app-company-entry-description-admin',
  standalone: true,
  imports: [SharedModule, MatButtonModule, MatCardModule, MatDialogModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressBarModule, MatSelectModule],
  templateUrl: './company-entry-description-admin.component.html',
  styleUrls: ['./company-entry-description-admin.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CompanyEntryDescriptionAdminComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CompanyEntryDescriptionsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly dialog = inject(MatDialog);

  rows: CompanyEntryDescriptionItem[] = [];
  loading = false;
  loadError = false;
  saving = false;
  deleting = false;
  operationError = '';
  successMessage = '';
  editingId: number | null = null;

  readonly columnas: ColDef[] = [
    { field: 'id', headerName: 'Identificador', sortable: true, filter: 'agNumberColumnFilter', maxWidth: 110 },
    { field: 'term', headerName: 'Término', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'description', headerName: 'Descripción', sortable: true, filter: 'agTextColumnFilter', flex: 1 },
    { field: 'sec', headerName: 'Clase (SEC)', sortable: true, filter: 'agTextColumnFilter', maxWidth: 120 },
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
        editar.setAttribute('aria-label', 'Editar registro ' + String(params.data?._original?.term ?? ''));
        editar.addEventListener('click', () => this.edit(params.data._original));
        const eliminar = document.createElement('button');
        eliminar.type = 'button';
        eliminar.classList.add('link');
        eliminar.innerText = 'Eliminar';
        eliminar.setAttribute('aria-label', 'Eliminar registro ' + String(params.data?._original?.term ?? ''));
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
    term: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(12)]],
    description: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(255)]],
    standardEntryClassCode: ['PPD' as 'PPD' | 'CCD', [Validators.required]],
    isActive: [true, [Validators.required]]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.loadError = false;
    this.api.list().subscribe({
      next: (rows) => {
        this.rows = rows ?? [];
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No pudimos cargar los conceptos de lote.');
        this.loadError = true;
        this.rows = [];
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
    this.operationError = '';
    this.cdr.markForCheck();
  }

  cancelEdit(): void {
    this.editingId = null;
    this.form.reset({ term: '', description: '', standardEntryClassCode: 'PPD', isActive: true });
    this.cdr.markForCheck();
  }

  submit(): void {
    if (this.saving || this.deleting || this.loading) return;
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
    this.operationError = '';
    this.successMessage = '';

    const request$ = this.editingId
      ? this.api.update(this.editingId, payload)
      : this.api.create(payload);

    request$.subscribe({
      next: () => {
        this.successMessage = this.editingId ? 'Los cambios del concepto se guardaron correctamente.' : 'El concepto se agregó correctamente.';
        this.notifications.success(this.successMessage);
        this.saving = false;
        this.cancelEdit();
        this.load();
      },
      error: (err) => {
        this.operationError = catalogErrorMessage(err, 'No pudimos guardar el concepto. Revisa los datos e inténtalo nuevamente.');
        this.notifications.error(this.operationError);
        this.saving = false;
        this.cdr.markForCheck();
      }
    });
  }

  remove(row: CompanyEntryDescriptionItem): void {
    if (this.saving || this.deleting || this.loading) return;

    this.dialog.open<CatalogActionConfirmDialogComponent, CatalogActionConfirmDialogData, boolean>(
      CatalogActionConfirmDialogComponent,
      {
        width: 'min(92vw, 440px)',
        data: {
          title: 'Eliminar concepto de lote',
          message: `¿Quieres eliminar el término ${row.term}? Esta acción no se puede deshacer.`,
          confirmText: 'Eliminar concepto',
          destructive: true
        }
      }
    ).afterClosed().pipe(take(1)).subscribe(confirmed => {
      if (!confirmed || this.deleting || this.saving) return;
      this.deleting = true;
      this.operationError = '';
      this.successMessage = '';
      this.cdr.markForCheck();
      this.api.delete(row.id).subscribe({
        next: () => {
          this.deleting = false;
          this.successMessage = 'El concepto se eliminó correctamente.';
          this.notifications.success(this.successMessage);
          if (this.editingId === row.id) this.cancelEdit();
          this.load();
        },
        error: err => {
          this.deleting = false;
          this.operationError = catalogErrorMessage(err, 'No pudimos eliminar el concepto. Inténtalo de nuevo.');
          this.notifications.error(this.operationError);
          this.cdr.markForCheck();
        }
      });
    });
  }
}
