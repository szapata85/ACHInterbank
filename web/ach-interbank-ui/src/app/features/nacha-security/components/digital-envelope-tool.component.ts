import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { SobreDigitalService } from '../services/sobre-digital.service';

@Component({
  selector: 'app-digital-envelope-tool',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
  templateUrl: './digital-envelope-tool.component.html',
  styleUrls: ['./digital-envelope-tool.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DigitalEnvelopeToolComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(SobreDigitalService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  encrypting = false;
  decrypting = false;

  readonly encryptForm = this.fb.group({
    file: [null as File | null, Validators.required]
  });

  readonly decryptForm = this.fb.group({
    file: [null as File | null, Validators.required]
  });

  onFileSelected(formType: 'encrypt' | 'decrypt', event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    if (formType === 'encrypt') {
      this.encryptForm.patchValue({ file });
      this.encryptForm.markAsDirty();
    } else {
      this.decryptForm.patchValue({ file });
      this.decryptForm.markAsDirty();
    }
  }

  submitEncrypt(): void {
    if (this.encryptForm.invalid) {
      this.encryptForm.markAllAsTouched();
      return;
    }

    const file = this.encryptForm.value.file;
    if (!file) {
      return;
    }

    this.encrypting = true;
    this.cdr.markForCheck();

    this.service
      .encrypt(file)
      .pipe(finalize(() => {
        this.encrypting = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (response) => {
          const fallbackName = `${file.name}.ENV`;
          this.downloadResponse(response, fallbackName);
          this.encryptForm.reset();
        },
        error: () => {
          this.notifications.error('No fue posible cifrar el archivo.');
        }
      });
  }

  submitDecrypt(): void {
    if (this.decryptForm.invalid) {
      this.decryptForm.markAllAsTouched();
      return;
    }

    const file = this.decryptForm.value.file;
    if (!file) {
      return;
    }

    this.decrypting = true;
    this.cdr.markForCheck();

    this.service
      .decrypt(file)
      .pipe(finalize(() => {
        this.decrypting = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (response) => {
          const defaultName = file.name.replace(/\.env$/i, '') || `NACHA_${this.buildTimestamp()}.txt`;
          this.downloadResponse(response, defaultName);
          this.decryptForm.reset();
        },
        error: () => {
          this.notifications.error('No fue posible descifrar el archivo.');
        }
      });
  }

  private downloadResponse(response: { body: Blob | null; headers: { get: (name: string) => string | null } }, fallbackName: string): void {
    const fileName = this.extractFileName(response.headers.get('content-disposition')) ?? fallbackName;
    const blob = response.body ?? new Blob();
    const url = window.URL.createObjectURL(blob);

    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();

    window.URL.revokeObjectURL(url);
  }

  private extractFileName(contentDisposition: string | null): string | null {
    if (!contentDisposition) {
      return null;
    }

    const match = /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i.exec(contentDisposition);
    const fileName = match?.[1] ?? match?.[2];

    return fileName ? decodeURIComponent(fileName) : null;
  }

  private buildTimestamp(): string {
    const now = new Date();
    const pad = (value: number) => value.toString().padStart(2, '0');
    return `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
  }
}
