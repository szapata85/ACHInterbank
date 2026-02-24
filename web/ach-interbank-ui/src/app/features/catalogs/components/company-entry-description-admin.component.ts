import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { CompanyEntryDescriptionItem } from '../models/company-entry-description.model';
import { CompanyEntryDescriptionsApiService } from '../services/company-entry-descriptions-api.service';

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
