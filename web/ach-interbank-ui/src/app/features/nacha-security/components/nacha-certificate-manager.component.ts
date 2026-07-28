import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  Inject,
  OnInit,
  inject
} from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { finalize, switchMap } from 'rxjs';
import { OperationalErrorView } from '../../../core/models/operational-error.model';
import { AuthService } from '../../../core/services/auth.service';
import { BlobDownloadService } from '../../../core/services/blob-download.service';
import { NotificationService } from '../../../core/services/notification.service';
import { presentNachaError } from '../../../core/utils/nacha-error-presentation.util';
import { SharedModule } from '../../../shared/shared.module';
import { ClearingHouseOption } from '../../ach-cycles/models/ach-cycle.model';
import { ClearingHousesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import { CertificateListItem } from '../models/certificate-management.model';
import { DigitalEnvelopeCertificateType } from '../models/digital-envelope-certificate.model';
import { NACHA_SECURITY_PERMISSIONS } from '../nacha-security-permissions';
import {
  certificateDaysRemaining,
  certificateEnvironmentCode,
  certificateEnvironmentLabel,
  certificateHolderCode,
  certificateHolderLabel,
  certificatePurposeCode,
  certificatePurposeLabel,
  certificateStatusClass,
  certificateStatusLabel,
  certificateValidityMessage,
  effectiveCertificateStatus,
  maskCertificateThumbprint,
  normalizedCertificateStatus
} from '../presentation/certificate-presentation';
import {
  CertificateManagementApiService,
  CertificateUploadContext
} from '../services/certificate-management-api.service';

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

interface CertificateActionDialogData {
  mode: 'activate' | 'revoke';
  certificate: CertificateListItem;
}

@Component({
  selector: 'app-nacha-certificate-manager',
  templateUrl: './nacha-certificate-manager.component.html',
  styleUrls: ['./nacha-certificate-manager.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    SharedModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatDividerModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule
  ]
})
export class NachaCertificateManagerComponent implements OnInit {
  private readonly certificatesService = inject(CertificateManagementApiService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly clearingHousesApi = inject(ClearingHousesApiService);
  private readonly downloads = inject(BlobDownloadService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly auth = inject(AuthService);
  readonly fileInputs: Partial<Record<DigitalEnvelopeCertificateType, HTMLInputElement>> = {};

  readonly displayedColumns = ['name', 'context', 'purpose', 'validity', 'status', 'privateKey', 'actions'];
  readonly purposeOptions = [
    { value: '', label: 'Todos los propósitos' },
    { value: 'OutboundEncryption', label: 'Cifrado de salida' },
    { value: 'InboundDecryption', label: 'Descifrado de entrada' },
    { value: 'OutboundSigning', label: 'Firma de salida' },
    { value: 'InboundSignatureValidation', label: 'Validación de firma de entrada' }
  ];
  readonly holderOptions = [
    { value: '', label: 'Todos los titulares' },
    { value: 'ClearingHouse', label: 'Cámara compensadora' },
    { value: 'Participant', label: 'Entidad participante' },
    { value: 'ThirdPartyProvider', label: 'Proveedor de servicios' }
  ];
  readonly statusOptions = [
    { value: '', label: 'Todos los estados' },
    { value: 'Draft', label: 'Borrador' },
    { value: 'Active', label: 'Activo' },
    { value: 'Inactive', label: 'Inactivo' },
    { value: 'Expired', label: 'Vencido' },
    { value: 'Replaced', label: 'Reemplazado' },
    { value: 'Revoked', label: 'Revocado' }
  ];

  uploadingType?: DigitalEnvelopeCertificateType;
  actionCertificateId?: number;
  loading = false;
  loadingClearingHouses = false;
  showUploadPanel = false;
  operationError: OperationalErrorView | null = null;
  certificates: CertificateListItem[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  lastUpdatedAt: Date | null = null;

  readonly contextForm = this.fb.group({
    clearingHouseId: [null as number | null, Validators.required],
    environment: ['Test' as 'Test' | 'Production', Validators.required],
    purpose: [''],
    holderType: [''],
    status: [''],
    search: ['']
  });

  readonly slots: CertificateSlot[] = [
    {
      type: 'EncryptionPublic',
      title: 'Certificado para cifrado de salida',
      description: 'Certificado público de la cámara utilizado para proteger los sobres digitales.',
      requiresPassword: false,
      accept: '.cer,.crt,.pem',
      allowedExtensions: ['.cer', '.crt', '.pem'],
      code: 'CAMARA-OUTBOUND-ENCRYPTION',
      displayName: 'Cámara - cifrado de salida',
      clearingHouseId: 0,
      environment: 'Test',
      purpose: 'OutboundEncryption',
      holderType: 'ClearingHouse'
    },
    {
      type: 'SigningKeyPair',
      title: 'Certificado para firma de salida',
      description: 'Identidad privada protegida de CFA utilizada para firmar archivos NACHA-M.',
      requiresPassword: true,
      accept: '.pfx,.p12',
      allowedExtensions: ['.pfx', '.p12'],
      code: 'CFA-CAMARA-OUTBOUND-SIGNING',
      displayName: 'CFA - firma de salida por cámara',
      clearingHouseId: 0,
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

  get canManage(): boolean {
    return this.auth.hasPermission([
      NACHA_SECURITY_PERMISSIONS.canManageCertificates,
      NACHA_SECURITY_PERMISSIONS.canManageAch
    ]);
  }

  get visibleCertificates(): CertificateListItem[] {
    const { purpose, holderType, status, search } = this.contextForm.getRawValue();
    const normalizedSearch = search?.trim().toLocaleLowerCase('es-CO') ?? '';
    return this.certificates.filter(certificate => {
      const purposeMatches = !purpose || certificatePurposeCode(certificate.purpose) === purpose;
      const holderMatches = !holderType || certificateHolderCode(certificate.holderType) === holderType;
      const statusMatches = !status || effectiveCertificateStatus(certificate) === status;
      const textMatches = !normalizedSearch || [
        certificate.displayName,
        certificate.fileName,
        certificate.thumbprint,
        certificate.fingerprintSha256
      ].some(value => value?.toLocaleLowerCase('es-CO').includes(normalizedSearch));
      return purposeMatches && holderMatches && statusMatches && textMatches;
    });
  }

  get summary(): { active: number; expiring: number; expired: number; draft: number; retired: number } {
    const statuses = this.certificates.map(certificate => effectiveCertificateStatus(certificate));
    return {
      active: statuses.filter(status => status === 'Active').length,
      expiring: this.expiringCertificates.length,
      expired: statuses.filter(status => status === 'Expired').length,
      draft: statuses.filter(status => status === 'Draft').length,
      retired: statuses.filter(status => status === 'Revoked' || status === 'Replaced').length
    };
  }

  get expiringCertificates(): CertificateListItem[] {
    return this.certificates.filter(certificate => {
      const days = certificateDaysRemaining(certificate);
      return effectiveCertificateStatus(certificate) === 'Active'
        && days !== null
        && days >= 0
        && days <= 30;
    });
  }

  ngOnInit(): void {
    this.loadClearingHouses();
  }

  applyFilters(): void {
    this.contextForm.markAllAsTouched();
    if (this.contextForm.invalid || this.loading) {
      return;
    }
    this.syncSlotContext();
    this.loadCertificates();
  }

  clearFilters(): void {
    this.contextForm.patchValue({
      purpose: '',
      holderType: '',
      status: '',
      search: ''
    });
  }

  refresh(): void {
    if (!this.loading) {
      this.loadCertificates();
    }
  }

  clearingHouseName(id: number): string {
    return this.clearingHouses.find(item => item.id === id)?.name ?? 'Cámara no disponible';
  }

  purposeLabel = certificatePurposeLabel;
  holderLabel = certificateHolderLabel;
  environmentLabel = certificateEnvironmentLabel;
  statusClass = certificateStatusClass;
  validityMessage = certificateValidityMessage;
  thumbprintSummary = maskCertificateThumbprint;

  statusLabel(certificate: CertificateListItem): string {
    return certificateStatusLabel(effectiveCertificateStatus(certificate));
  }

  canActivate(certificate: CertificateListItem): boolean {
    return this.canManage
      && !['Active', 'Revoked', 'Replaced', 'Expired'].includes(effectiveCertificateStatus(certificate));
  }

  canRevoke(certificate: CertificateListItem): boolean {
    return this.canManage && normalizedCertificateStatus(certificate.status) === 'Active';
  }

  openDetails(certificate: CertificateListItem): void {
    this.dialog.open(CertificateDetailsDialogComponent, {
      width: 'min(760px, calc(100vw - 2rem))',
      maxHeight: 'calc(100dvh - 2rem)',
      autoFocus: 'dialog',
      restoreFocus: true,
      data: {
        certificate,
        clearingHouseName: this.clearingHouseName(certificate.clearingHouseId)
      }
    });
  }

  requestActivate(certificate: CertificateListItem): void {
    if (!this.canActivate(certificate) || this.actionCertificateId) {
      return;
    }
    const ref = this.dialog.open(CertificateActionDialogComponent, {
      width: 'min(520px, calc(100vw - 2rem))',
      autoFocus: 'dialog',
      restoreFocus: true,
      data: { mode: 'activate', certificate } satisfies CertificateActionDialogData
    });
    ref.afterClosed().subscribe(confirmed => {
      if (confirmed === true) {
        this.activate(certificate);
      }
    });
  }

  requestRevoke(certificate: CertificateListItem): void {
    if (!this.canRevoke(certificate) || this.actionCertificateId) {
      return;
    }
    const ref = this.dialog.open(CertificateActionDialogComponent, {
      width: 'min(520px, calc(100vw - 2rem))',
      autoFocus: 'dialog',
      restoreFocus: true,
      data: { mode: 'revoke', certificate } satisfies CertificateActionDialogData
    });
    ref.afterClosed().subscribe(reason => {
      if (typeof reason === 'string' && reason.trim()) {
        this.revoke(certificate, reason.trim());
      }
    });
  }

  async copyThumbprint(certificate: CertificateListItem): Promise<void> {
    if (!certificate.thumbprint) {
      return;
    }
    try {
      await navigator.clipboard.writeText(certificate.thumbprint);
      this.notifications.success('Huella digital copiada.');
    } catch {
      this.notifications.error('No fue posible copiar la huella digital.');
    }
  }

  onFileSelected(type: DigitalEnvelopeCertificateType, event: Event): void {
    this.operationError = null;
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

  removeSelectedFile(type: DigitalEnvelopeCertificateType): void {
    this.forms[type].get('file')?.reset();
    const input = this.fileInputs[type];
    if (input) {
      input.value = '';
    }
  }

  selectedFileName(type: DigitalEnvelopeCertificateType): string | null {
    return (this.forms[type].get('file')?.value as File | null)?.name ?? null;
  }

  upload(type: DigitalEnvelopeCertificateType): void {
    if (this.uploadingType || this.actionCertificateId) {
      return;
    }
    this.operationError = null;
    const form = this.forms[type];
    if (form.invalid || this.contextForm.invalid) {
      form.markAllAsTouched();
      this.contextForm.markAllAsTouched();
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

    request.pipe(finalize(() => {
      this.uploadingType = undefined;
      form.get('password')?.reset('');
      this.cdr.markForCheck();
    })).subscribe({
      next: () => {
        this.notifications.success(`${file.name} se cargó correctamente como nueva versión en borrador.`);
        form.reset();
        this.removeSelectedFile(type);
        this.loadCertificates();
      },
      error: error => void this.handleError(error, 'No se pudo cargar el certificado.')
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
    return control?.touched && control.hasError('required')
      ? 'Ingresa la contraseña del archivo privado.'
      : undefined;
  }

  trackCertificate(_: number, certificate: CertificateListItem): number {
    return certificate.id;
  }

  private activate(certificate: CertificateListItem): void {
    this.actionCertificateId = certificate.id;
    this.operationError = null;
    this.certificatesService.validate(certificate.id).pipe(
      switchMap(validation => {
        const isValid = validation.isValid ?? validation.canActivate ?? validation.errors.length === 0;
        if (!isValid) {
          throw new Error(validation.errors.join(' '));
        }
        return this.certificatesService.activate(certificate.id);
      }),
      finalize(() => {
        this.actionCertificateId = undefined;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: () => {
        this.notifications.success('La versión del certificado quedó activa.');
        this.loadCertificates();
      },
      error: error => void this.handleError(error, 'No fue posible activar el certificado.')
    });
  }

  private revoke(certificate: CertificateListItem, reason: string): void {
    this.actionCertificateId = certificate.id;
    this.operationError = null;
    this.certificatesService.revoke(certificate.id, reason).pipe(finalize(() => {
      this.actionCertificateId = undefined;
      this.cdr.markForCheck();
    })).subscribe({
      next: () => {
        this.notifications.success('El certificado fue revocado y el motivo quedó registrado.');
        this.loadCertificates();
      },
      error: error => void this.handleError(error, 'No fue posible revocar el certificado.')
    });
  }

  private getSlot(type: DigitalEnvelopeCertificateType): CertificateSlot {
    return this.slots.find(slot => slot.type === type)!;
  }

  private loadClearingHouses(): void {
    this.loadingClearingHouses = true;
    this.clearingHousesApi.list().pipe(finalize(() => {
      this.loadingClearingHouses = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: items => {
        this.clearingHouses = items;
        this.contextForm.controls.clearingHouseId.setValue(items[0]?.id ?? null, { emitEvent: false });
        this.syncSlotContext();
        this.loadCertificates();
      },
      error: error => void this.handleError(error, 'No se pudieron consultar las cámaras compensadoras.')
    });
  }

  private syncSlotContext(): void {
    const clearingHouseId = Number(this.contextForm.controls.clearingHouseId.value ?? 0);
    const selectedHouse = this.clearingHouses.find(item => item.id === clearingHouseId);
    const chamberCode = this.normalizeClearingHouseCode(selectedHouse, clearingHouseId);
    const chamberName = selectedHouse?.name ?? 'Cámara sin seleccionar';
    const environment = this.contextForm.controls.environment.value ?? 'Test';

    for (const slot of this.slots) {
      slot.clearingHouseId = clearingHouseId;
      slot.environment = environment;
      if (slot.type === 'EncryptionPublic') {
        slot.code = `${chamberCode}-OUTBOUND-ENCRYPTION`;
        slot.displayName = `${chamberName} - cifrado de salida`;
        slot.description = `Certificado público de ${chamberName} utilizado para cifrar sobres digitales.`;
      } else {
        slot.code = `CFA-${chamberCode}-OUTBOUND-SIGNING`;
        slot.displayName = `CFA - firma de salida ${chamberName}`;
        slot.description = `Identidad privada protegida de CFA para firmar archivos dirigidos a ${chamberName}.`;
      }
    }
  }

  private normalizeClearingHouseCode(selectedHouse: ClearingHouseOption | undefined, clearingHouseId: number): string {
    const code = selectedHouse?.code?.trim().toUpperCase().replace(/[^A-Z0-9_-]+/g, '-');
    return code || `CAMARA-${clearingHouseId || 'SIN-SELECCION'}`;
  }

  private loadCertificates(): void {
    const clearingHouseId = Number(this.contextForm.controls.clearingHouseId.value ?? 0);
    if (!clearingHouseId || this.loading) {
      return;
    }
    this.loading = true;
    this.operationError = null;
    this.cdr.markForCheck();

    this.certificatesService.list({
      clearingHouseId,
      environment: this.contextForm.controls.environment.value ?? 'Test'
    }).pipe(finalize(() => {
      this.loading = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: certificates => {
        this.certificates = certificates;
        this.lastUpdatedAt = new Date();
        this.slots.forEach(slot => {
          slot.certificate = certificates
            .filter(certificate =>
              certificatePurposeCode(certificate.purpose) === slot.purpose
              && certificateHolderCode(certificate.holderType) === slot.holderType
              && certificateEnvironmentCode(certificate.environment) === slot.environment)
            .sort((left, right) => right.versionNumber - left.versionNumber)[0];
        });
      },
      error: error => void this.handleError(error, 'No se pudieron obtener los certificados.')
    });
  }

  private async handleError(error: unknown, fallback: string): Promise<void> {
    const parsed = await this.downloads.fromHttpError(error, fallback);
    this.operationError = presentNachaError(parsed, fallback);
    this.notifications.error(`${this.operationError.title}. ${this.operationError.message}`);
    this.cdr.markForCheck();
  }
}

@Component({
  selector: 'app-certificate-details-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatChipsModule,
    MatDialogModule,
    MatDividerModule,
    MatIconModule,
    MatTooltipModule
  ],
  template: `
    <h2 mat-dialog-title>Detalles del certificado</h2>
    <mat-dialog-content>
      <section>
        <h3>Identificación</h3>
        <dl>
          <div><dt>Nombre</dt><dd>{{ data.certificate.displayName }}</dd></div>
          <div><dt>Versión</dt><dd>{{ data.certificate.versionNumber }}</dd></div>
          <div><dt>Número de serie</dt><dd class="technical">{{ data.certificate.serialNumber }}</dd></div>
          <div class="wide"><dt>Huella digital</dt><dd class="technical">{{ data.certificate.thumbprint }}</dd></div>
          <div class="wide"><dt>Huella SHA-256</dt><dd class="technical">{{ data.certificate.fingerprintSha256 }}</dd></div>
        </dl>
      </section>
      <mat-divider></mat-divider>
      <section>
        <h3>Uso</h3>
        <dl>
          <div><dt>Cámara compensadora</dt><dd>{{ data.clearingHouseName }}</dd></div>
          <div><dt>Ambiente</dt><dd>{{ environmentLabel(data.certificate.environment) }}</dd></div>
          <div><dt>Propósito</dt><dd>{{ purposeLabel(data.certificate.purpose) }}</dd></div>
          <div><dt>Tipo de titular</dt><dd>{{ holderLabel(data.certificate.holderType) }}</dd></div>
        </dl>
      </section>
      <mat-divider></mat-divider>
      <section>
        <h3>Vigencia y seguridad</h3>
        <dl>
          <div><dt>Vigente desde</dt><dd>{{ data.certificate.notBefore | date:'dd/MM/yyyy HH:mm':'UTC' }}</dd></div>
          <div><dt>Vigente hasta</dt><dd>{{ data.certificate.notAfter | date:'dd/MM/yyyy HH:mm':'UTC' }}</dd></div>
          <div><dt>Estado</dt><dd>{{ statusLabel(data.certificate) }}</dd></div>
          <div><dt>Material privado</dt><dd>{{ data.certificate.hasPrivateKey ? 'Disponible en almacenamiento seguro' : 'Solo contiene clave pública' }}</dd></div>
          <div><dt>Algoritmo</dt><dd>{{ data.certificate.keyAlgorithm }} · {{ data.certificate.keySize }} bits</dd></div>
          <div><dt>Algoritmo de firma</dt><dd>{{ data.certificate.signatureAlgorithm }}</dd></div>
          <div class="wide"><dt>Emisor</dt><dd>{{ data.certificate.issuer }}</dd></div>
          <div class="wide"><dt>Sujeto</dt><dd>{{ data.certificate.subject }}</dd></div>
        </dl>
      </section>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-flat-button mat-dialog-close type="button">Cerrar</button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content{min-width:0}section{padding:.25rem 0 1rem}section+section{padding-top:1rem}h3{margin:.25rem 0 .75rem;font-size:1rem}
    dl{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:.8rem 1.25rem;margin:0}.wide{grid-column:1/-1}dt{color:var(--color-text-muted);font-size:.78rem}dd{margin:.2rem 0 0;overflow-wrap:anywhere}
    .technical{font-family:ui-monospace,SFMono-Regular,Consolas,monospace;font-size:.82rem}
    @media(max-width:600px){dl{grid-template-columns:1fr}.wide{grid-column:auto}}
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CertificateDetailsDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) readonly data: {
    certificate: CertificateListItem;
    clearingHouseName: string;
  }) {}

  purposeLabel = certificatePurposeLabel;
  holderLabel = certificateHolderLabel;
  environmentLabel = certificateEnvironmentLabel;

  statusLabel(certificate: CertificateListItem): string {
    return certificateStatusLabel(effectiveCertificateStatus(certificate));
  }
}

@Component({
  selector: 'app-certificate-action-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'activate' ? 'Activar certificado' : 'Revocar certificado' }}</h2>
    <mat-dialog-content>
      <p *ngIf="data.mode === 'activate'">
        ¿Deseas activar esta versión del certificado?
      </p>
      <p *ngIf="data.mode === 'activate'" class="help">
        Si existe otra versión activa con el mismo propósito, quedará marcada como reemplazada.
      </p>
      <form *ngIf="data.mode === 'revoke'" [formGroup]="form">
        <p>La revocación impide que esta versión vuelva a utilizarse en operaciones criptográficas.</p>
        <mat-form-field appearance="outline">
          <mat-label>Motivo de la revocación</mat-label>
          <textarea matInput formControlName="reason" rows="3" maxlength="500"></textarea>
          <mat-hint align="end">{{ form.controls.reason.value.length }}/500</mat-hint>
          <mat-error *ngIf="form.controls.reason.hasError('required')">El motivo es obligatorio.</mat-error>
          <mat-error *ngIf="form.controls.reason.hasError('minlength')">Describe el motivo con al menos 10 caracteres.</mat-error>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="dialogRef.close()">Cancelar</button>
      <button
        [attr.color]="data.mode === 'revoke' ? 'warn' : null"
        mat-flat-button
        type="button"
        [disabled]="data.mode === 'revoke' && form.invalid"
        (click)="confirm()">
        {{ data.mode === 'activate' ? 'Activar versión' : 'Revocar certificado' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-form-field{width:100%;margin-top:.5rem}.help{color:var(--color-text-muted)}p{line-height:1.5}
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CertificateActionDialogComponent {
  private readonly fb = inject(FormBuilder);
  readonly form = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]]
  });

  constructor(
    @Inject(MAT_DIALOG_DATA) readonly data: CertificateActionDialogData,
    readonly dialogRef: MatDialogRef<CertificateActionDialogComponent>
  ) {}

  confirm(): void {
    if (this.data.mode === 'activate') {
      this.dialogRef.close(true);
      return;
    }
    this.form.markAllAsTouched();
    if (this.form.valid) {
      this.dialogRef.close(this.form.controls.reason.value.trim());
    }
  }
}
