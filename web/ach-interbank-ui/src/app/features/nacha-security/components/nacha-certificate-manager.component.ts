import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message.util';
import { SharedModule } from '../../../shared/shared.module';
import { CertificateListItem } from '../models/certificate-management.model';
import {
  CertificateManagementApiService,
  CertificateUploadContext
} from '../services/certificate-management-api.service';
import { DigitalEnvelopeCertificateType } from '../models/digital-envelope-certificate.model';

const MAX_CERTIFICATE_SIZE = 10 * 1024 * 1024;

interface CertificateSlot extends CertificateUploadContext {
  type: DigitalEnvelopeCertificateType;
  title: string;
  description: string;
  requiresPassword: boolean;
  accept: string;
  allowedExtensions: readonly string[];
  certificate?: CertificateListItem;
}

@Component({
  selector: 'app-nacha-certificate-manager',
  templateUrl: './nacha-certificate-manager.component.html',
  styleUrls: ['./nacha-certificate-manager.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, SharedModule]
})
export class NachaCertificateManagerComponent implements OnInit {
  private readonly certificatesService = inject(CertificateManagementApiService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly fileInputs: Partial<Record<DigitalEnvelopeCertificateType, HTMLInputElement>> = {};

  uploadingType?: DigitalEnvelopeCertificateType;
  loading = false;
  error?: string;
  success?: string;

  readonly slots: CertificateSlot[] = [
    {
      type: 'EncryptionPublic',
      title: 'Certificado de cifrado (llave pública)',
      description: 'Certificado público de ACH Colombia usado para cifrar los sobres digitales.',
      requiresPassword: false,
      accept: '.cer,.crt,.pem',
      allowedExtensions: ['.cer', '.crt', '.pem'],
      code: 'ACHCOL-OUTBOUND-ENCRYPTION',
      displayName: 'ACH Colombia - cifrado saliente',
      clearingHouseId: 1,
      environment: 'Test',
      purpose: 'OutboundEncryption',
      holderType: 'ClearingHouse'
    },
    {
      type: 'SigningKeyPair',
      title: 'Certificado de firma (par público/privado)',
      description: 'Certificado y llave privada de CFA usados para firma de archivos NACHA-M.',
      requiresPassword: true,
      accept: '.pfx,.p12',
      allowedExtensions: ['.pfx', '.p12'],
      code: 'CFA-OUTBOUND-SIGNING',
      displayName: 'CFA - firma saliente',
      clearingHouseId: 1,
      environment: 'Test',
      purpose: 'OutboundSigning',
      holderType: 'Participant'
    }
  ];

  readonly forms: Record<DigitalEnvelopeCertificateType, FormGroup> = {
    EncryptionPublic: this.fb.group({ file: [null, Validators.required] }),
    SigningKeyPair: this.fb.group({
      file: [null, Validators.required],
      password: ['', Validators.required]
    })
  } as Record<DigitalEnvelopeCertificateType, FormGroup>;

  ngOnInit(): void {
    this.loadCertificates();
  }

  onFileSelected(type: DigitalEnvelopeCertificateType, event: Event): void {
    this.clearMessages();
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    const control = this.forms[type].get('file');
    this.fileInputs[type] = input;
    control?.setValue(file);
    control?.markAsTouched();

    if (!file) {
      control?.setErrors({ required: true });
      return;
    }

    const slot = this.getSlot(type);
    const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
    if (!slot.allowedExtensions.includes(extension)) {
      control?.setErrors({ extension: true });
    } else if (file.size === 0) {
      control?.setErrors({ empty: true });
    } else if (file.size > MAX_CERTIFICATE_SIZE) {
      control?.setErrors({ maxSize: true });
    } else {
      control?.setErrors(null);
    }

    this.cdr.markForCheck();
  }

  upload(type: DigitalEnvelopeCertificateType): void {
    if (this.uploadingType) {
      return;
    }

    this.clearMessages();
    const form = this.forms[type];
    if (form.invalid) {
      form.markAllAsTouched();
      this.error = 'Revisa el archivo y los campos obligatorios antes de continuar.';
      this.cdr.markForCheck();
      return;
    }

    const file = form.get('file')?.value as File | null;
    if (!file) {
      return;
    }

    const slot = this.getSlot(type);
    const password = String(form.get('password')?.value ?? '');
    this.uploadingType = type;
    this.cdr.markForCheck();

    const request = type === 'EncryptionPublic'
      ? this.certificatesService.uploadPublic(slot, file)
      : this.certificatesService.uploadPrivate(slot, file, password);

    request
      .pipe(
        finalize(() => {
          this.uploadingType = undefined;
          form.get('password')?.reset('');
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (certificate) => {
          slot.certificate = certificate;
          this.success = `${file.name} se cargó correctamente.`;
          form.reset();
          const input = this.fileInputs[type];
          if (input) {
            input.value = '';
          }
          this.loadCertificates();
        },
        error: (err) => {
          this.error = getHttpErrorMessage(err, 'No se pudo cargar el certificado.');
          this.cdr.markForCheck();
        }
      });
  }

  fileError(type: DigitalEnvelopeCertificateType): string | undefined {
    const control = this.forms[type].get('file');
    if (!control?.touched || !control.errors) {
      return undefined;
    }
    if (control.errors['extension']) return `Formato no permitido. Usa ${this.getSlot(type).accept}.`;
    if (control.errors['empty']) return 'El archivo está vacío.';
    if (control.errors['maxSize']) return 'El archivo supera el máximo de 10 MB.';
    if (control.errors['required']) return 'Selecciona un archivo.';
    return 'El archivo no es válido.';
  }

  passwordError(type: DigitalEnvelopeCertificateType): string | undefined {
    const control = this.forms[type].get('password');
    return control?.touched && control.hasError('required') ? 'Ingresa la contraseña del archivo privado.' : undefined;
  }

  private getSlot(type: DigitalEnvelopeCertificateType): CertificateSlot {
    return this.slots.find((slot) => slot.type === type)!;
  }

  private loadCertificates(): void {
    this.loading = true;
    this.cdr.markForCheck();

    this.certificatesService
      .list()
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (certificates) => {
          this.slots.forEach((slot) => {
            slot.certificate = certificates.find((certificate) => certificate.code === slot.code);
          });
          this.cdr.markForCheck();
        },
        error: (err) => {
          if (!this.error) {
            this.error = getHttpErrorMessage(err, 'No se pudieron obtener los certificados.');
          }
          this.cdr.markForCheck();
        }
      });
  }

  private clearMessages(): void {
    this.error = undefined;
    this.success = undefined;
  }
}
