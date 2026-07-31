import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  Inject,
  OnInit,
  inject
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import {
  MAT_DIALOG_DATA,
  MatDialog,
  MatDialogModule,
  MatDialogRef
} from '@angular/material/dialog';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatStepper, MatStepperModule } from '@angular/material/stepper';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { forkJoin, finalize } from 'rxjs';
import { BlobDownloadService } from '../../../core/services/blob-download.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { ClearingHouseOption } from '../../ach-cycles/models/ach-cycle.model';
import { ClearingHousesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import { DestinationInstitution } from '../../transactions/transactions.models';
import { FinancialInstitutionsApiService } from '../../transactions/services/financial-institutions-api.service';
import {
  CertificateFunctionalStatus,
  CertificateListItem,
  CertificatePreview,
  ManagedCertificatePurpose
} from '../models/certificate-management.model';
import {
  CertificateManagementApiService,
  ManagedCertificateUpload
} from '../services/certificate-management-api.service';

const MAX_CERTIFICATE_SIZE = 10 * 1024 * 1024;

const STATUS_TEXT: Readonly<Record<string, { label: string; description: string }>> = {
  PendingValidity: {
    label: 'Pendiente de vigencia',
    description: 'Este certificado todavía no ha llegado a su fecha de inicio.'
  },
  Valid: {
    label: 'Vigente',
    description: 'Este certificado puede utilizarse normalmente.'
  },
  ExpiringSoon: {
    label: 'Próximo a vencer',
    description: 'Este certificado requiere renovación próximamente.'
  },
  Expired: {
    label: 'Vencido',
    description: 'Este certificado ya no puede utilizarse.'
  },
  Revoked: {
    label: 'Revocado',
    description: 'Este certificado fue deshabilitado manualmente y no puede utilizarse.'
  },
  Replaced: {
    label: 'Reemplazado',
    description: 'Este certificado fue sustituido por uno más reciente.'
  },
  Inactive: {
    label: 'No operativo',
    description: 'Este certificado no está habilitado para uso operativo.'
  }
};

const NUMERIC_FUNCTIONAL_STATUS: Readonly<Record<string, string>> = {
  '1': 'PendingValidity',
  '2': 'Valid',
  '3': 'ExpiringSoon',
  '4': 'Expired',
  '5': 'Revoked',
  '6': 'Replaced',
  '7': 'Inactive'
};

interface UploadDialogData {
  initialPurpose: ManagedCertificatePurpose;
  clearingHouses: ClearingHouseOption[];
  defaultInstitution: DestinationInstitution | null;
  initialClearingHouseId?: number | null;
}

interface CertificateActionDialogData {
  action: 'revoke' | 'delete';
  certificate: CertificateListItem;
  hasReplacement: boolean;
}

@Component({
  selector: 'app-nacha-certificate-manager',
  templateUrl: './nacha-certificate-manager.component.html',
  styleUrls: ['./nacha-certificate-manager.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    SharedModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatExpansionModule,
    MatIconModule,
    MatMenuModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    MatTooltipModule
  ]
})
export class NachaCertificateManagerComponent implements OnInit {
  private readonly api = inject(CertificateManagementApiService);
  private readonly clearingHousesApi = inject(ClearingHousesApiService);
  private readonly financialInstitutionsApi = inject(FinancialInstitutionsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly downloads = inject(BlobDownloadService);
  private readonly dialog = inject(MatDialog);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = true;
  actionCertificateId?: number;
  certificates: CertificateListItem[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  defaultInstitution: DestinationInstitution | null = null;
  errorMessage = '';

  ngOnInit(): void {
    this.load();
  }

  get cfaCertificates(): CertificateListItem[] {
    return this.certificates.filter(certificate =>
      certificate.financialInstitutionId != null
      || certificate.hasPrivateKey
      || this.purposeCode(certificate.purpose) === 'CfaSigningAndDecryption');
  }

  get clearingHouseCertificates(): CertificateListItem[] {
    return this.certificates.filter(certificate => !this.cfaCertificates.includes(certificate));
  }

  get operationalCfaCertificate(): CertificateListItem | undefined {
    return this.cfaCertificates.find(certificate => this.isOperational(certificate));
  }

  get operationalClearingHouseCount(): number {
    return new Set(
      this.clearingHouseCertificates
        .filter(certificate => this.isOperational(certificate))
        .map(certificate => certificate.clearingHouseId)
        .filter((id): id is number => id != null)
    ).size;
  }

  get expiringCount(): number {
    return this.certificates.filter(certificate =>
      this.functionalStatusCode(certificate.functionalStatus) === 'ExpiringSoon'
    ).length;
  }

  get unavailableCount(): number {
    return this.certificates.filter(certificate =>
      ['Expired', 'Revoked'].includes(this.functionalStatusCode(certificate.functionalStatus))
    ).length;
  }

  hasOperationalCertificateForHouse(clearingHouseId: number): boolean {
    return this.clearingHouseCertificates.some(certificate =>
      certificate.clearingHouseId === clearingHouseId && this.isOperational(certificate));
  }

  load(): void {
    this.loading = true;
    this.errorMessage = '';
    forkJoin({
      certificates: this.api.list(),
      clearingHouses: this.clearingHousesApi.list(),
      institutions: this.financialInstitutionsApi.getAll()
    }).pipe(finalize(() => {
      this.loading = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: result => {
        this.certificates = result.certificates;
        this.clearingHouses = result.clearingHouses.filter(item => item.isActive !== false);
        this.defaultInstitution = result.institutions.find(item => item.isDefaultSource) ?? null;
      },
      error: error => void this.presentError(error, 'No fue posible consultar los certificados.')
    });
  }

  openUpload(
    purpose: ManagedCertificatePurpose,
    clearingHouseId?: number | null
  ): void {
    const ref = this.dialog.open(CertificateUploadDialogComponent, {
      width: 'min(860px, calc(100vw - 2rem))',
      maxHeight: 'calc(100dvh - 2rem)',
      autoFocus: 'dialog',
      restoreFocus: true,
      data: {
        initialPurpose: purpose,
        clearingHouses: this.clearingHouses,
        defaultInstitution: this.defaultInstitution,
        initialClearingHouseId: clearingHouseId
      } satisfies UploadDialogData
    });
    ref.afterClosed().subscribe(saved => {
      if (saved === true) {
        this.notifications.success('El certificado se guardó correctamente.');
        this.load();
      }
    });
  }

  openDetails(certificate: CertificateListItem): void {
    this.dialog.open(CertificateDetailsDialogComponent, {
      width: 'min(760px, calc(100vw - 2rem))',
      maxHeight: 'calc(100dvh - 2rem)',
      autoFocus: 'dialog',
      restoreFocus: true,
      data: certificate
    });
  }

  requestRevoke(certificate: CertificateListItem): void {
    const ref = this.dialog.open(CertificateActionDialogComponent, {
      width: 'min(560px, calc(100vw - 2rem))',
      autoFocus: 'dialog',
      restoreFocus: true,
      data: {
        action: 'revoke',
        certificate,
        hasReplacement: this.hasOperationalReplacement(certificate)
      } satisfies CertificateActionDialogData
    });
    ref.afterClosed().subscribe(reason => {
      if (typeof reason === 'string' && reason.trim()) {
        this.revoke(certificate, reason.trim());
      }
    });
  }

  requestDelete(certificate: CertificateListItem): void {
    const ref = this.dialog.open(CertificateActionDialogComponent, {
      width: 'min(560px, calc(100vw - 2rem))',
      autoFocus: 'dialog',
      restoreFocus: true,
      data: {
        action: 'delete',
        certificate,
        hasReplacement: this.hasOperationalReplacement(certificate)
      } satisfies CertificateActionDialogData
    });
    ref.afterClosed().subscribe(confirmed => {
      if (confirmed === true) {
        this.delete(certificate);
      }
    });
  }

  canRevoke(certificate: CertificateListItem): boolean {
    return this.isOperational(certificate);
  }

  actionLabel(certificate: CertificateListItem): string {
    return this.functionalStatusCode(certificate.functionalStatus) === 'ExpiringSoon'
      ? 'Renovar o reemplazar'
      : ['Expired', 'Revoked', 'Replaced'].includes(this.functionalStatusCode(certificate.functionalStatus))
        ? 'Cargar reemplazo'
        : 'Reemplazar certificado';
  }

  ownerName(certificate: CertificateListItem): string {
    return certificate.financialInstitutionName
      ?? certificate.clearingHouseName
      ?? this.clearingHouses.find(item => item.id === certificate.clearingHouseId)?.name
      ?? 'Propietario no disponible';
  }

  useLabel(certificate: CertificateListItem): string {
    return this.cfaCertificates.includes(certificate)
      ? 'Firmar y descifrar información de CFA'
      : 'Validar información recibida';
  }

  statusLabel(certificate: CertificateListItem): string {
    return STATUS_TEXT[this.functionalStatusCode(certificate.functionalStatus)]?.label ?? 'No operativo';
  }

  statusDescription(certificate: CertificateListItem): string {
    return STATUS_TEXT[this.functionalStatusCode(certificate.functionalStatus)]?.description
      ?? 'Este certificado no está habilitado para uso operativo.';
  }

  statusClass(certificate: CertificateListItem): string {
    return `status-${this.functionalStatusCode(certificate.functionalStatus)
      .replace(/([a-z])([A-Z])/g, '$1-$2')
      .toLowerCase()}`;
  }

  timeRemaining(certificate: CertificateListItem): string {
    const status = this.functionalStatusCode(certificate.functionalStatus);
    if (status === 'Expired') return 'Vigencia finalizada';
    if (certificate.daysRemaining == null) return 'No disponible';
    if (certificate.daysRemaining === 0) return 'Vence hoy';
    if (certificate.daysRemaining === 1) return '1 día';
    return `${certificate.daysRemaining} días`;
  }

  technicalPanelLabel(certificate: CertificateListItem): string {
    return `Información técnica de ${certificate.displayName}`;
  }

  trackCertificate(_: number, certificate: CertificateListItem): number {
    return certificate.id;
  }

  private isOperational(certificate: CertificateListItem): boolean {
    return ['Valid', 'ExpiringSoon'].includes(
      this.functionalStatusCode(certificate.functionalStatus)
    );
  }

  private hasOperationalReplacement(certificate: CertificateListItem): boolean {
    return this.certificates.some(candidate =>
      candidate.id !== certificate.id
      && this.isOperational(candidate)
      && candidate.financialInstitutionId === certificate.financialInstitutionId
      && candidate.clearingHouseId === certificate.clearingHouseId);
  }

  private purposeCode(value: string | number): string {
    const numeric: Readonly<Record<string, string>> = {
      '1': 'OutboundEncryption',
      '2': 'InboundDecryption',
      '3': 'OutboundSigning',
      '4': 'InboundSignatureValidation',
      '5': 'CfaSigningAndDecryption',
      '6': 'ClearingHouseValidation'
    };
    return numeric[String(value)] ?? String(value);
  }

  private functionalStatusCode(value: CertificateFunctionalStatus): string {
    return NUMERIC_FUNCTIONAL_STATUS[String(value)] ?? String(value);
  }

  private revoke(certificate: CertificateListItem, reason: string): void {
    this.actionCertificateId = certificate.id;
    this.api.revoke(certificate.id, reason).pipe(finalize(() => {
      this.actionCertificateId = undefined;
      this.cdr.markForCheck();
    })).subscribe({
      next: () => {
        this.notifications.success('El certificado fue revocado y su historial se conservó.');
        this.load();
      },
      error: error => void this.presentError(error, 'No fue posible revocar el certificado.')
    });
  }

  private delete(certificate: CertificateListItem): void {
    this.actionCertificateId = certificate.id;
    this.api.delete(certificate.id).pipe(finalize(() => {
      this.actionCertificateId = undefined;
      this.cdr.markForCheck();
    })).subscribe({
      next: () => {
        this.notifications.success('El certificado se eliminó de forma segura.');
        this.load();
      },
      error: error => void this.presentError(error, 'No fue posible eliminar el certificado.')
    });
  }

  private async presentError(error: unknown, fallback: string): Promise<void> {
    const parsed = await this.downloads.fromHttpError(error, fallback);
    this.errorMessage = parsed.message || fallback;
    this.notifications.error(this.errorMessage);
    this.cdr.markForCheck();
  }
}

@Component({
  selector: 'app-certificate-upload-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatRadioModule,
    MatSelectModule,
    MatStepperModule,
    MatTooltipModule
  ],
  template: `
    <div class="upload-dialog">
      <div class="dialog-heading">
        <div>
          <span class="eyebrow">Carga guiada</span>
          <h2 mat-dialog-title>Agregar certificado de seguridad</h2>
        </div>
        <button mat-icon-button type="button" mat-dialog-close aria-label="Cerrar diálogo">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <mat-dialog-content>
        <mat-stepper linear #stepper>
          <mat-step [stepControl]="purposeForm" label="¿Para qué se utilizará?">
            <form [formGroup]="purposeForm" class="step-content">
              <mat-radio-group formControlName="purpose" aria-label="Uso del certificado">
                <mat-radio-button value="CfaSigningAndDecryption">
                  <strong>Firmar y descifrar información de CFA</strong>
                  <span>Identifica a CFA, permite firmar información enviada y descifrar archivos dirigidos a la entidad.</span>
                </mat-radio-button>
                <mat-radio-button value="ClearingHouseValidation">
                  <strong>Validar información recibida de una cámara compensadora</strong>
                  <span>Comprueba que la información fue emitida por la cámara compensadora seleccionada.</span>
                </mat-radio-button>
              </mat-radio-group>
              @if (purposeForm.controls.purpose.touched && purposeForm.controls.purpose.hasError('required')) {
                <mat-error>Selecciona el uso del certificado.</mat-error>
              }
              <div class="step-actions">
                <button mat-button type="button" mat-dialog-close>Cancelar</button>
                <button mat-flat-button type="button" matStepperNext (click)="syncOwner()">Continuar</button>
              </div>
            </form>
          </mat-step>

          <mat-step [stepControl]="ownerForm" label="¿A quién pertenece?">
            <form [formGroup]="ownerForm" class="step-content">
              @if (isCfaPurpose) {
                <div class="owner-summary">
                  <mat-icon>account_balance</mat-icon>
                  <div>
                    <span>Entidad propietaria</span>
                    <strong>{{ data.defaultInstitution?.name || 'Entidad de origen no configurada' }}</strong>
                    <p>La entidad financiera configurada actualmente como origen es CFA.</p>
                  </div>
                </div>
                @if (!data.defaultInstitution) {
                  <mat-error>No se encontró una entidad financiera configurada como origen.</mat-error>
                }
              } @else {
                <mat-form-field appearance="outline">
                  <mat-label>Cámara compensadora</mat-label>
                  <mat-select formControlName="clearingHouseId">
                    @for (house of data.clearingHouses; track house.id) {
                      <mat-option [value]="house.id">{{ house.name }}</mat-option>
                    }
                  </mat-select>
                  @if (ownerForm.controls.clearingHouseId.hasError('required')) {
                    <mat-error>Selecciona la cámara compensadora propietaria del certificado.</mat-error>
                  }
                </mat-form-field>
              }
              <div class="step-actions">
                <button mat-button type="button" matStepperPrevious>Anterior</button>
                <button
                  mat-flat-button
                  type="button"
                  matStepperNext
                  [disabled]="ownerForm.invalid || (isCfaPurpose && !data.defaultInstitution)">
                  Continuar
                </button>
              </div>
            </form>
          </mat-step>

          <mat-step [stepControl]="fileForm" label="Seleccionar archivo">
            <form [formGroup]="fileForm" class="step-content" novalidate>
              <label class="file-picker" for="managed-certificate-file">
                <input
                  #fileInput
                  id="managed-certificate-file"
                  type="file"
                  [accept]="acceptedFormats"
                  (change)="onFileSelected($event)">
                <mat-icon>upload_file</mat-icon>
                <span>
                  <strong>{{ selectedFile?.name || 'Archivo del certificado' }}</strong>
                  <small>Formatos permitidos: {{ acceptedFormats }} · máximo 10 MB</small>
                </span>
              </label>
              @if (fileError) {
                <mat-error>{{ fileError }}</mat-error>
              }

              @if (isCfaPurpose) {
                <mat-form-field appearance="outline">
                  <mat-label>Contraseña del certificado</mat-label>
                  <input
                    matInput
                    [type]="showPassword ? 'text' : 'password'"
                    formControlName="password"
                    autocomplete="new-password">
                  <button
                    mat-icon-button
                    matSuffix
                    type="button"
                    (click)="showPassword = !showPassword"
                    [attr.aria-label]="showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                    [matTooltip]="showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'">
                    <mat-icon>{{ showPassword ? 'visibility_off' : 'visibility' }}</mat-icon>
                  </button>
                  @if (fileForm.controls.password.hasError('required')) {
                    <mat-error>Ingresa la contraseña del certificado.</mat-error>
                  }
                </mat-form-field>
                <p class="password-help">
                  La contraseña se utilizará únicamente para abrir y validar el certificado. No se almacenará ni se mostrará posteriormente.
                </p>
              }

              @if (errorMessage) {
                <div class="dialog-error" role="alert">{{ errorMessage }}</div>
              }
              <div class="step-actions">
                <button mat-button type="button" matStepperPrevious>Anterior</button>
                <button
                  mat-flat-button
                  type="button"
                  [disabled]="fileForm.invalid || verifying"
                  (click)="verify(stepper)">
                  @if (verifying) {
                    <mat-spinner diameter="20"></mat-spinner>
                    Verificando…
                  } @else {
                    Verificar información
                  }
                </button>
              </div>
            </form>
          </mat-step>

          <mat-step label="Verificar información">
            <section class="step-content verification">
              @if (preview) {
                <div class="validation-result" [class.validation-result--warning]="preview.warnings.length">
                  <mat-icon>{{ preview.isValid ? 'verified' : 'warning' }}</mat-icon>
                  <div>
                    <strong>{{ preview.isValid ? 'El certificado cumple las validaciones requeridas.' : 'Revisa las advertencias antes de continuar.' }}</strong>
                    @for (warning of preview.warnings; track warning) {
                      <p>{{ warning }}</p>
                    }
                  </div>
                </div>
                <dl class="verification-grid">
                  <div><dt>Propietario</dt><dd>{{ ownerName }}</dd></div>
                  <div><dt>Uso del certificado</dt><dd>{{ useLabel }}</dd></div>
                  <div><dt>Titular del certificado</dt><dd>{{ preview.subject }}</dd></div>
                  <div><dt>Emitido por</dt><dd>{{ preview.issuer }}</dd></div>
                  <div><dt>Válido desde</dt><dd>{{ preview.notBefore | date:'dd/MM/yyyy':'UTC' }}</dd></div>
                  <div><dt>Válido hasta</dt><dd>{{ preview.notAfter | date:'dd/MM/yyyy':'UTC' }}</dd></div>
                  <div><dt>Estado detectado</dt><dd>{{ previewStatusLabel }}</dd></div>
                  <div><dt>Permite firmar y descifrar</dt><dd>{{ preview.canSignAndDecrypt ? 'Sí' : 'No aplica' }}</dd></div>
                  <div><dt>Identificador digital</dt><dd class="technical-value">{{ preview.thumbprint }}</dd></div>
                </dl>
              }
              @if (errorMessage) {
                <div class="dialog-error" role="alert">{{ errorMessage }}</div>
              }
              <div class="step-actions">
                <button mat-button type="button" matStepperPrevious [disabled]="saving">Anterior</button>
                <button mat-button type="button" mat-dialog-close [disabled]="saving">Cancelar</button>
                <button mat-flat-button type="button" (click)="save()" [disabled]="!preview?.isValid || saving">
                  @if (saving) {
                    <mat-spinner diameter="20"></mat-spinner>
                    Guardando…
                  } @else {
                    Guardar certificado
                  }
                </button>
              </div>
            </section>
          </mat-step>
        </mat-stepper>
      </mat-dialog-content>
    </div>
  `,
  styles: [`
    .upload-dialog { min-width: 0; }
    .dialog-heading { display:flex; align-items:flex-start; justify-content:space-between; padding:1.25rem 1.5rem 0; }
    .dialog-heading h2 { margin:.2rem 0 0; }
    .eyebrow { color:#6a1b9a; font-size:.75rem; font-weight:700; letter-spacing:.08em; text-transform:uppercase; }
    .step-content { display:grid; gap:1rem; padding:1.25rem .25rem .25rem; min-height:260px; }
    mat-radio-group { display:grid; gap:.75rem; }
    mat-radio-button { border:1px solid #d7dce5; border-radius:14px; padding:1rem; }
    mat-radio-button strong, mat-radio-button span { display:block; white-space:normal; }
    mat-radio-button span { color:#52606d; margin-top:.35rem; }
    .owner-summary, .validation-result { display:flex; gap:1rem; border-radius:14px; background:#f4f7fb; padding:1rem; }
    .owner-summary span, .owner-summary strong { display:block; }
    .owner-summary p { margin:.35rem 0 0; color:#52606d; }
    .file-picker { display:flex; align-items:center; gap:1rem; border:2px dashed #aab4c3; border-radius:14px; padding:1.25rem; cursor:pointer; }
    .file-picker input { position:absolute; opacity:0; pointer-events:none; }
    .file-picker span, .file-picker strong, .file-picker small { display:block; }
    .file-picker small, .password-help { color:#52606d; }
    .password-help { margin:-.5rem 0 0; font-size:.85rem; }
    .step-actions { display:flex; justify-content:flex-end; gap:.5rem; margin-top:auto; flex-wrap:wrap; }
    .step-actions button mat-spinner { display:inline-block; margin-right:.5rem; }
    .verification-grid { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:.75rem; margin:0; }
    .verification-grid div { background:#f7f8fa; border-radius:10px; padding:.75rem; }
    .verification-grid dt { color:#52606d; font-size:.8rem; }
    .verification-grid dd { margin:.25rem 0 0; overflow-wrap:anywhere; }
    .technical-value { font-family:ui-monospace,SFMono-Regular,Consolas,monospace; font-size:.78rem; }
    .validation-result { background:#eef8f2; }
    .validation-result--warning { background:#fff6df; }
    .validation-result p { margin:.35rem 0 0; }
    .dialog-error { background:#fff0f0; color:#9b1c1c; border-radius:10px; padding:.75rem; }
    @media (max-width:640px) {
      .dialog-heading { padding:1rem 1rem 0; }
      .verification-grid { grid-template-columns:1fr; }
      .step-content { min-height:0; }
    }
  `]
})
export class CertificateUploadDialogComponent {
  readonly data = inject<UploadDialogData>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CertificateManagementApiService);
  private readonly downloads = inject(BlobDownloadService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly dialogRef = inject(MatDialogRef<CertificateUploadDialogComponent>);

  selectedFile: File | null = null;
  preview: CertificatePreview | null = null;
  verifying = false;
  saving = false;
  showPassword = false;
  fileError = '';
  errorMessage = '';

  readonly purposeForm = this.fb.nonNullable.group({
    purpose: [this.data.initialPurpose, Validators.required]
  });
  readonly ownerForm = this.fb.group({
    clearingHouseId: [this.data.initialClearingHouseId ?? null as number | null]
  });
  readonly fileForm = this.fb.group({
    file: [null as File | null, Validators.required],
    password: ['']
  });

  constructor() {
    this.syncOwner();
  }

  get isCfaPurpose(): boolean {
    return this.purposeForm.controls.purpose.value === 'CfaSigningAndDecryption';
  }

  get acceptedFormats(): string {
    return this.isCfaPurpose ? '.pfx, .p12' : '.cer, .crt, .pem';
  }

  get ownerName(): string {
    return this.isCfaPurpose
      ? this.data.defaultInstitution?.name ?? 'Entidad de origen no configurada'
      : this.data.clearingHouses.find(
          item => item.id === this.ownerForm.controls.clearingHouseId.value
        )?.name ?? 'Cámara no seleccionada';
  }

  get useLabel(): string {
    return this.isCfaPurpose
      ? 'Firmar y descifrar información de CFA'
      : 'Validar información recibida de una cámara compensadora';
  }

  get previewStatusLabel(): string {
    const code = NUMERIC_FUNCTIONAL_STATUS[String(this.preview?.functionalStatus)]
      ?? String(this.preview?.functionalStatus ?? 'Inactive');
    return STATUS_TEXT[code]?.label ?? 'No operativo';
  }

  syncOwner(): void {
    if (this.isCfaPurpose) {
      this.ownerForm.controls.clearingHouseId.clearValidators();
      this.ownerForm.controls.clearingHouseId.setValue(null);
      this.fileForm.controls.password.setValidators(Validators.required);
    } else {
      this.ownerForm.controls.clearingHouseId.setValidators(Validators.required);
      this.fileForm.controls.password.clearValidators();
      this.fileForm.controls.password.setValue('');
    }
    this.ownerForm.controls.clearingHouseId.updateValueAndValidity();
    this.fileForm.controls.password.updateValueAndValidity();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFile = file;
    this.fileForm.controls.file.setValue(file);
    this.fileForm.controls.file.markAsTouched();
    this.fileError = '';
    this.preview = null;

    if (!file) {
      this.fileError = 'Selecciona un archivo para continuar.';
      return;
    }
    const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
    const allowed = this.isCfaPurpose
      ? ['.pfx', '.p12']
      : ['.cer', '.crt', '.pem'];
    if (!allowed.includes(extension)) {
      this.fileError = 'El certificado no es compatible con el uso seleccionado.';
      this.fileForm.controls.file.setErrors({ extension: true });
    } else if (file.size === 0) {
      this.fileError = 'El archivo seleccionado está vacío.';
      this.fileForm.controls.file.setErrors({ empty: true });
    } else if (file.size > MAX_CERTIFICATE_SIZE) {
      this.fileError = 'El archivo supera el máximo de 10 MB.';
      this.fileForm.controls.file.setErrors({ maxSize: true });
    } else {
      this.fileForm.controls.file.setErrors(null);
    }
  }

  verify(stepper: MatStepper): void {
    this.errorMessage = '';
    this.fileForm.markAllAsTouched();
    if (this.fileForm.invalid || !this.selectedFile) {
      if (!this.selectedFile) this.fileError = 'Selecciona un archivo para continuar.';
      this.focusFirstInvalid();
      return;
    }

    this.verifying = true;
    this.api.preview(this.request()).pipe(finalize(() => {
      this.verifying = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: preview => {
        this.preview = preview;
        stepper.next();
      },
      error: error => void this.presentError(error, 'No fue posible verificar el certificado.')
    });
  }

  save(): void {
    if (!this.preview?.isValid || this.saving || !this.selectedFile) return;
    this.saving = true;
    this.errorMessage = '';
    this.api.save(this.request()).pipe(finalize(() => {
      this.saving = false;
      this.clearPassword();
      this.cdr.markForCheck();
    })).subscribe({
      next: () => this.dialogRef.close(true),
      error: error => void this.presentError(error, 'No fue posible guardar el certificado.')
    });
  }

  private request(): ManagedCertificateUpload {
    return {
      purpose: this.purposeForm.controls.purpose.value,
      clearingHouseId: this.ownerForm.controls.clearingHouseId.value,
      file: this.selectedFile!,
      password: this.isCfaPurpose ? this.fileForm.controls.password.value ?? '' : undefined
    };
  }

  private clearPassword(): void {
    this.fileForm.controls.password.setValue('');
    this.showPassword = false;
  }

  private focusFirstInvalid(): void {
    queueMicrotask(() => {
      const element = document.querySelector(
        '.mat-mdc-dialog-container .ng-invalid input, .mat-mdc-dialog-container .ng-invalid button'
      ) as HTMLElement | null;
      element?.focus();
    });
  }

  private async presentError(error: unknown, fallback: string): Promise<void> {
    const parsed = await this.downloads.fromHttpError(error, fallback);
    this.errorMessage = parsed.message || fallback;
    this.cdr.markForCheck();
  }
}

@Component({
  selector: 'app-certificate-details-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatDialogModule,
    MatExpansionModule,
    MatIconModule
  ],
  template: `
    <div class="details-dialog">
      <div class="dialog-title-row">
        <div>
          <span>Detalle del certificado</span>
          <h2 mat-dialog-title>{{ certificate.displayName }}</h2>
        </div>
        <button mat-icon-button mat-dialog-close aria-label="Cerrar diálogo">
          <mat-icon>close</mat-icon>
        </button>
      </div>
      <mat-dialog-content>
        <dl class="details-grid">
          <div><dt>Entidad propietaria</dt><dd>{{ owner }}</dd></div>
          <div><dt>Uso del certificado</dt><dd>{{ use }}</dd></div>
          <div><dt>Estado</dt><dd>{{ status }}</dd></div>
          <div><dt>Válido desde</dt><dd>{{ certificate.notBefore | date:'dd/MM/yyyy':'UTC' }}</dd></div>
          <div><dt>Válido hasta</dt><dd>{{ certificate.notAfter | date:'dd/MM/yyyy':'UTC' }}</dd></div>
          <div><dt>Fecha de carga</dt><dd>{{ certificate.uploadedAtUtc | date:'dd/MM/yyyy, HH:mm':'UTC' }}</dd></div>
          @if (certificate.revokedAtUtc) {
            <div><dt>Fecha de revocación</dt><dd>{{ certificate.revokedAtUtc | date:'dd/MM/yyyy, HH:mm':'UTC' }}</dd></div>
            <div><dt>Motivo de la revocación</dt><dd>{{ certificate.revocationReason }}</dd></div>
          }
        </dl>
        <mat-expansion-panel>
          <mat-expansion-panel-header>
            <mat-panel-title>Información técnica</mat-panel-title>
          </mat-expansion-panel-header>
          <dl class="technical-grid">
            <div><dt>Titular del certificado</dt><dd>{{ certificate.subject }}</dd></div>
            <div><dt>Emitido por</dt><dd>{{ certificate.issuer }}</dd></div>
            <div><dt>Identificador digital (Thumbprint)</dt><dd>{{ certificate.thumbprint }}</dd></div>
            <div><dt>Número de identificación</dt><dd>{{ certificate.serialNumber }}</dd></div>
            <div><dt>Algoritmo</dt><dd>{{ certificate.keyAlgorithm }}</dd></div>
            <div><dt>Tamaño de llave</dt><dd>{{ certificate.keySize }} bits</dd></div>
            <div><dt>Tiene llave privada</dt><dd>{{ certificate.hasPrivateKey ? 'Sí, protegida' : 'No' }}</dd></div>
            <div><dt>Formato del archivo</dt><dd>{{ certificate.fileName }}</dd></div>
            <div><dt>Identificador interno</dt><dd>{{ certificate.id }}</dd></div>
          </dl>
        </mat-expansion-panel>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-flat-button mat-dialog-close>Cerrar</button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .details-dialog { padding-top:1rem; }
    .dialog-title-row { display:flex; justify-content:space-between; align-items:flex-start; padding:0 1.5rem; }
    .dialog-title-row span { color:#6b7280; }
    .dialog-title-row h2 { margin:.2rem 0 0; }
    .details-grid, .technical-grid { display:grid; grid-template-columns:repeat(2,minmax(0,1fr)); gap:.75rem; margin:0 0 1rem; }
    .details-grid div, .technical-grid div { background:#f7f8fa; border-radius:10px; padding:.75rem; }
    dt { color:#52606d; font-size:.8rem; } dd { margin:.25rem 0 0; overflow-wrap:anywhere; }
    @media (max-width:640px) { .details-grid, .technical-grid { grid-template-columns:1fr; } }
  `]
})
export class CertificateDetailsDialogComponent {
  constructor(
    @Inject(MAT_DIALOG_DATA) readonly certificate: CertificateListItem
  ) {}

  get owner(): string {
    return this.certificate.financialInstitutionName
      ?? this.certificate.clearingHouseName
      ?? 'Propietario no disponible';
  }

  get use(): string {
    return this.certificate.hasPrivateKey
      ? 'Firmar y descifrar información de CFA'
      : 'Validar información recibida';
  }

  get status(): string {
    const code = NUMERIC_FUNCTIONAL_STATUS[String(this.certificate.functionalStatus)]
      ?? String(this.certificate.functionalStatus);
    return STATUS_TEXT[code]?.label ?? 'No operativo';
  }
}

@Component({
  selector: 'app-certificate-action-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule
  ],
  template: `
    <div class="action-dialog">
      <h2 mat-dialog-title>
        {{ data.action === 'revoke' ? 'Revocar certificado' : 'Eliminar certificado' }}
      </h2>
      <mat-dialog-content>
        <div class="affected-certificate">
          <mat-icon>{{ data.action === 'revoke' ? 'block' : 'delete' }}</mat-icon>
          <div>
            <strong>{{ data.certificate.displayName }}</strong>
            <span>{{ owner }}</span>
          </div>
        </div>
        @if (data.action === 'revoke') {
          <p>Al revocar este certificado dejará de utilizarse para firmar, validar o descifrar información. Su historial se conservará.</p>
          @if (!data.hasReplacement) {
            <p class="impact">{{ operationalImpact }}</p>
          }
          <mat-form-field appearance="outline">
            <mat-label>Motivo de la revocación</mat-label>
            <textarea matInput [formControl]="reason" rows="3"></textarea>
            @if (reason.hasError('required')) {
              <mat-error>Ingresa el motivo de la revocación.</mat-error>
            }
          </mat-form-field>
        } @else {
          <p>Este certificado nunca ha sido utilizado y puede eliminarse de forma segura. Esta acción retirará el registro del sistema.</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button mat-dialog-close>Cancelar</button>
        @if (data.action === 'revoke') {
          <button mat-flat-button class="danger" (click)="confirmRevoke()" [disabled]="reason.invalid">
            Revocar
          </button>
        } @else {
          <button mat-flat-button class="danger" [mat-dialog-close]="true">
            Eliminar certificado
          </button>
        }
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .action-dialog { padding-top:1rem; }
    .affected-certificate { display:flex; gap:.75rem; align-items:center; background:#f7f8fa; border-radius:12px; padding:.9rem; }
    .affected-certificate strong, .affected-certificate span { display:block; }
    .affected-certificate span { color:#52606d; margin-top:.2rem; }
    .impact { background:#fff4df; border-left:4px solid #d97706; padding:.75rem; }
    mat-form-field { width:100%; }
    .danger { background:#b42318 !important; color:white !important; }
  `]
})
export class CertificateActionDialogComponent {
  readonly reason = inject(FormBuilder).nonNullable.control('', [
    Validators.required,
    Validators.maxLength(500)
  ]);
  private readonly dialogRef = inject(MatDialogRef<CertificateActionDialogComponent>);

  constructor(@Inject(MAT_DIALOG_DATA) readonly data: CertificateActionDialogData) {}

  get owner(): string {
    return this.data.certificate.financialInstitutionName
      ?? this.data.certificate.clearingHouseName
      ?? 'Propietario no disponible';
  }

  get operationalImpact(): string {
    return this.data.certificate.hasPrivateKey
      ? 'Si continúas, CFA quedará sin un certificado vigente para firmar o descifrar información.'
      : 'Si continúas, no habrá un certificado vigente para validar la información recibida de esta cámara compensadora.';
  }

  confirmRevoke(): void {
    this.reason.markAsTouched();
    if (this.reason.valid) {
      this.dialogRef.close(this.reason.value.trim());
    }
  }
}
