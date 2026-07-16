import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { SharedModule } from '../../../shared/shared.module';
import { NotificationService } from '../../../core/services/notification.service';
import { SobreDigitalCertificate, SobreDigitalService } from '../services/sobre-digital.service';
import { sanitizeDownloadFileName } from '../utils/download-file-name.util';

@Component({
  selector: 'app-digital-envelope-tool',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule],
  templateUrl: './digital-envelope-tool.component.html',
  styleUrls: ['./digital-envelope-tool.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DigitalEnvelopeToolComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(SobreDigitalService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  encrypting = false;
  decrypting = false;
  loadingCertificates = true;
  encryptionCertificates: SobreDigitalCertificate[] = [];
  decryptionCertificates: SobreDigitalCertificate[] = [];
  lastEncryptedFileName: string | null = null;
  lastDecryptedFileName: string | null = null;

  readonly encryptForm = this.fb.group({
    certificateVersionId: [null as number | null, Validators.required],
    file: [null as File | null, Validators.required]
  });

  readonly decryptForm = this.fb.group({
    certificateVersionId: [null as number | null, Validators.required],
    file: [null as File | null, Validators.required]
  });

  ngOnInit(): void {
    this.service.listCertificates().subscribe({
      next: (certificates) => {
        this.encryptionCertificates = certificates.filter((certificate) => certificate.canEncrypt);
        this.decryptionCertificates = certificates.filter((certificate) => certificate.canDecrypt);
        if (this.encryptionCertificates.length === 1) {
          this.encryptForm.patchValue({ certificateVersionId: this.encryptionCertificates[0].id });
        }
        if (this.decryptionCertificates.length === 1) {
          this.decryptForm.patchValue({ certificateVersionId: this.decryptionCertificates[0].id });
        }
        this.loadingCertificates = false;
        this.cdr.markForCheck();
      },
      error: (error: HttpErrorResponse) => {
        this.loadingCertificates = false;
        void this.notifyHttpError(error, 'No fue posible consultar los certificados activos.');
        this.cdr.markForCheck();
      }
    });
  }

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
    const certificateVersionId = this.encryptForm.value.certificateVersionId;
    if (!file || !certificateVersionId) {
      return;
    }

    this.encrypting = true;
    this.cdr.markForCheck();

    this.service
      .encrypt(file, certificateVersionId)
      .pipe(finalize(() => {
        this.encrypting = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (response) => {
          const fallbackName = `${file.name}.ENV`;
          this.lastEncryptedFileName = this.downloadResponse(response, fallbackName);
          this.notifications.success(`Archivo cifrado: ${this.lastEncryptedFileName}`);
          this.encryptForm.patchValue({ file: null });
          this.encryptForm.markAsPristine();
          this.cdr.markForCheck();
        },
        error: (error: HttpErrorResponse) => {
          void this.notifyHttpError(error, 'No fue posible cifrar el archivo.');
        }
      });
  }

  submitDecrypt(): void {
    if (this.decryptForm.invalid) {
      this.decryptForm.markAllAsTouched();
      return;
    }

    const file = this.decryptForm.value.file;
    const certificateVersionId = this.decryptForm.value.certificateVersionId;
    if (!file || !certificateVersionId) {
      return;
    }

    if (!file.name.toUpperCase().endsWith('.ENV')) {
      this.notifications.error('El archivo para descifrar debe terminar en .ENV.');
      return;
    }

    this.decrypting = true;
    this.cdr.markForCheck();

    this.service
      .decrypt(file, certificateVersionId)
      .pipe(finalize(() => {
        this.decrypting = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (response) => {
          const defaultName = file.name.replace(/\.env$/i, '') || `NACHA_${this.buildTimestamp()}.txt`;
          this.lastDecryptedFileName = this.downloadResponse(response, defaultName);
          this.notifications.success(`Archivo recuperado: ${this.lastDecryptedFileName}`);
          this.decryptForm.patchValue({ file: null });
          this.decryptForm.markAsPristine();
          this.cdr.markForCheck();
        },
        error: (error: HttpErrorResponse) => {
          void this.notifyHttpError(error, 'No fue posible descifrar el archivo.');
        }
      });
  }

  private downloadResponse(response: { body: Blob | null; headers: { get: (name: string) => string | null } }, fallbackName: string): string {
    const fileName = sanitizeDownloadFileName(this.extractFileName(response.headers.get('content-disposition')), fallbackName);
    const blob = response.body ?? new Blob();
    const url = window.URL.createObjectURL(blob);

    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.setTimeout(() => window.URL.revokeObjectURL(url), 0);
    return fileName;
  }

  private async notifyHttpError(error: HttpErrorResponse, fallback: string): Promise<void> {
    let message = fallback;
    if (error.error instanceof Blob && error.error.size > 0) {
      try {
        const problem = JSON.parse(await error.error.text()) as { detail?: string; title?: string };
        message = problem.detail || problem.title || fallback;
      } catch {
        // Keep the safe functional fallback; never render an HTML proxy error body.
      }
    } else if (typeof error.error?.detail === 'string') {
      message = error.error.detail;
    }
    this.notifications.error(message);
    this.cdr.markForCheck();
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
