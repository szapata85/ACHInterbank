import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import { NachaUploadRecord, NachaUploadService } from '../../services/nacha-upload.service';

@Component({
  selector: 'app-nacha-upload',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './nacha-upload.component.html',
  styleUrls: ['./nacha-upload.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaUploadComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly nachaUpload = inject(NachaUploadService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  uploading = false;
  loadingRecords = false;
  records: NachaUploadRecord[] = [];

  form = this.fb.group({
    file: [null as File | null, Validators.required]
  });

  filtersForm = this.fb.group({
    immediateOrigin: [''],
    immediateDestination: [''],
    referenceCode: [''],
    achCycleId: [''],
    fileCreationDate: ['']
  });

  ngOnInit(): void {
    this.loadRecords();
  }

  get fileName(): string {
    return this.form.get('file')?.value?.name ?? '';
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.form.patchValue({ file });
    this.form.markAsTouched();
  }

  upload(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifications.error('Selecciona un archivo NACHA-M para continuar.');
      return;
    }

    const file = this.form.get('file')?.value;
    if (!file) {
      this.notifications.error('Selecciona un archivo NACHA-M para continuar.');
      return;
    }

    this.uploading = true;
    this.nachaUpload
      .upload(file)
      .pipe(
        finalize(() => {
          this.uploading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (response) => {
          const message = this.extractUploadMessage(response);
          this.notifications.success(message || 'Archivo cargado correctamente.');
          this.form.reset();
          this.loadRecords();
        },
        error: () => {
          this.notifications.error('No fue posible cargar el archivo NACHA-M.');
        }
      });
  }

  searchRecords(): void {
    this.loadRecords();
  }

  clearFilters(): void {
    this.filtersForm.reset({
      immediateOrigin: '',
      immediateDestination: '',
      referenceCode: '',
      achCycleId: '',
      fileCreationDate: ''
    });
    this.loadRecords();
  }

  private loadRecords(): void {
    this.loadingRecords = true;
    const filters = this.filtersForm.getRawValue();

    this.nachaUpload
      .listRecords(filters)
      .pipe(
        finalize(() => {
          this.loadingRecords = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (records) => {
          this.records = records ?? [];
        },
        error: () => {
          this.records = [];
          this.notifications.error('No fue posible consultar el detalle de archivos NACHA-M cargados.');
        }
      });
  }

  private extractUploadMessage(response: unknown): string {
    if (typeof response === 'string') {
      return response;
    }

    if (response && typeof response === 'object' && 'message' in response) {
      const value = (response as { message?: unknown }).message;
      return typeof value === 'string' ? value : '';
    }

    return '';
  }
}
