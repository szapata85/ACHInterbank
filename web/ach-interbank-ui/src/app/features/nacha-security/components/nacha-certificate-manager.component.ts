import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { DigitalEnvelopeCertificatesService } from '../services/digital-envelope-certificates.service';
import { DigitalEnvelopeCertificate, DigitalEnvelopeCertificateType } from '../models/digital-envelope-certificate.model';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';

interface CertificateSlot {
  type: DigitalEnvelopeCertificateType;
  title: string;
  description: string;
  requiresPassword: boolean;
  certificate?: DigitalEnvelopeCertificate;
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
  private readonly certificatesService = inject(DigitalEnvelopeCertificatesService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  uploadingType?: DigitalEnvelopeCertificateType;
  loading = false;
  error?: string;

  readonly slots: CertificateSlot[] = [
    {
      type: 'EncryptionPublic',
      title: 'Certificado de cifrado (llave pública)',
      description: 'Se usa para cifrar los sobres digitales que recibirá la cámara compensadora.',
      requiresPassword: false
    },
    {
      type: 'SigningKeyPair',
      title: 'Certificado de firma (par público/privado)',
      description: 'Se usa para firmar los archivos NACHA-M y requiere la llave privada.',
      requiresPassword: true
    }
  ];

  forms: Record<DigitalEnvelopeCertificateType, FormGroup> = {
    EncryptionPublic: this.fb.group({
      file: [null, Validators.required]
    }),
    SigningKeyPair: this.fb.group({
      file: [null, Validators.required],
      password: ['']
    })
  } as Record<DigitalEnvelopeCertificateType, FormGroup>;

  ngOnInit(): void {
    this.loadCertificates();
  }

  onFileSelected(type: DigitalEnvelopeCertificateType, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const form = this.forms[type];
    form.patchValue({ file });
    form.markAsDirty();
  }

  upload(type: DigitalEnvelopeCertificateType): void {
    const form = this.forms[type];
    if (form.invalid) {
      form.markAllAsTouched();
      return;
    }

    const file = form.get('file')?.value as File | null;
    if (!file) {
      return;
    }

    const password = form.get('password')?.value as string | undefined;
    this.uploadingType = type;
    this.error = undefined;
    this.cdr.markForCheck();

    this.certificatesService
      .upload(type, file, password)
      .pipe(
        finalize(() => {
          this.uploadingType = undefined;
          this.loadCertificates();
        })
      )
      .subscribe({
        next: () => {
          form.reset();
        },
        error: (err) => {
          this.error = err?.error ?? 'No se pudo cargar el certificado.';
          this.cdr.markForCheck();
        }
      });
  }

  deleteCertificate(certificate: DigitalEnvelopeCertificate): void {
    this.loading = true;
    this.error = undefined;
    this.cdr.markForCheck();

    this.certificatesService
      .delete(certificate.id)
      .pipe(finalize(() => this.loadCertificates()))
      .subscribe({
        error: (err) => {
          this.error = err?.error ?? 'No se pudo eliminar el certificado.';
          this.cdr.markForCheck();
        }
      });
  }

  private loadCertificates(): void {
    this.loading = true;
    this.error = undefined;
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
            slot.certificate = certificates.find((c) => c.type === slot.type);
          });
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.error = err?.error ?? 'No se pudieron obtener los certificados.';
          this.cdr.markForCheck();
        }
      });
  }
}
