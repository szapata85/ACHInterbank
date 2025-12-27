import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import { NachaUploadService } from '../../services/nacha-upload.service';

@Component({
  selector: 'app-nacha-upload',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './nacha-upload.component.html',
  styleUrls: ['./nacha-upload.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaUploadComponent {
  private readonly fb = inject(FormBuilder);
  private readonly nachaUpload = inject(NachaUploadService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  uploading = false;

  form = this.fb.group({
    file: [null as File | null, Validators.required]
  });

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
        next: (message) => {
          this.notifications.success(message || 'Archivo cargado correctamente.');
          this.form.reset();
        },
        error: () => {
          this.notifications.error('No fue posible cargar el archivo NACHA-M.');
        }
      });
  }
}
