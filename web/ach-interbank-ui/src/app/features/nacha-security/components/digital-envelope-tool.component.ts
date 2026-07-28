import { CommonModule } from '@angular/common';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { OperationalErrorView } from '../../../core/models/operational-error.model';
import { BlobDownloadService, SavedDownload } from '../../../core/services/blob-download.service';
import { NotificationService } from '../../../core/services/notification.service';
import { presentNachaError } from '../../../core/utils/nacha-error-presentation.util';
import { SharedModule } from '../../../shared/shared.module';
import { ClearingHouseOption } from '../../ach-cycles/models/ach-cycle.model';
import { ClearingHousesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import {
  certificateEnvironmentCode,
  certificateEnvironmentLabel,
  certificatePurposeCode,
  certificatePurposeLabel
} from '../presentation/certificate-presentation';
import { SobreDigitalCertificate, SobreDigitalService } from '../services/sobre-digital.service';

const MAX_FILE_SIZE = 50 * 1024 * 1024;
type EnvelopeMode = 'encrypt' | 'decrypt';

interface EnvelopeResult {
  mode: EnvelopeMode;
  fileName: string;
  size: number;
  completedAt: Date;
  certificate: SobreDigitalCertificate;
  cryptographicProfile?: string | null;
}

@Component({
  selector: 'app-digital-envelope-tool',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    SharedModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDividerModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTabsModule,
    MatTooltipModule
  ],
  templateUrl: './digital-envelope-tool.component.html',
  styleUrls: ['./digital-envelope-tool.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DigitalEnvelopeToolComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(SobreDigitalService);
  private readonly clearingHousesApi = inject(ClearingHousesApiService);
  private readonly downloads = inject(BlobDownloadService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  processingMode: EnvelopeMode | null = null;
  loadingConfiguration = true;
  certificates: SobreDigitalCertificate[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  operationError: OperationalErrorView | null = null;
  result: EnvelopeResult | null = null;
  private lastResponse: HttpResponse<Blob> | null = null;
  private lastAttemptMode: EnvelopeMode = 'encrypt';

  readonly encryptForm = this.buildForm();
  readonly decryptForm = this.buildForm();

  ngOnInit(): void {
    forkJoin({
      certificates: this.service.listCertificates(),
      clearingHouses: this.clearingHousesApi.list()
    }).pipe(finalize(() => {
      this.loadingConfiguration = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: ({ certificates, clearingHouses }) => {
        this.certificates = certificates;
        this.clearingHouses = clearingHouses;
        this.initializeContext('encrypt');
        this.initializeContext('decrypt');
        this.registerAutomaticResolution('encrypt');
        this.registerAutomaticResolution('decrypt');
      },
      error: error => void this.handleError(error, 'No fue posible consultar la configuración criptográfica.')
    });
  }

  availableHouses(mode: EnvelopeMode): ClearingHouseOption[] {
    const ids = new Set(this.certificatesForMode(mode).map(certificate => certificate.clearingHouseId));
    return this.clearingHouses.filter(house => ids.has(house.id));
  }

  selectedCertificate(mode: EnvelopeMode): SobreDigitalCertificate | null {
    const id = this.formFor(mode).controls['certificateVersionId'].value as number | null;
    return this.certificates.find(certificate => certificate.id === id) ?? null;
  }

  configurationCandidates(mode: EnvelopeMode): SobreDigitalCertificate[] {
    const form = this.formFor(mode);
    const clearingHouseId = form.controls['clearingHouseId'].value as number | null;
    const environment = form.controls['environment'].value as string | null;
    return this.certificatesForMode(mode)
      .filter(certificate =>
        certificate.clearingHouseId === clearingHouseId
        && certificateEnvironmentCode(certificate.environment) === environment)
      .sort((left, right) => right.versionNumber - left.versionNumber || right.id - left.id);
  }

  hasDuplicateConfiguration(mode: EnvelopeMode): boolean {
    return this.configurationCandidates(mode).length > 1;
  }

  certificatePurposeLabel = certificatePurposeLabel;
  environmentLabel = certificateEnvironmentLabel;

  clearingHouseName(id: number): string {
    return this.clearingHouses.find(house => house.id === id)?.name ?? 'Cámara no disponible';
  }

  onFileSelected(mode: EnvelopeMode, event: Event): void {
    this.operationError = null;
    this.result = null;
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    const control = this.formFor(mode).controls['file'];
    control.setValue(file);
    control.markAsTouched();

    if (!file) {
      control.setErrors({ required: true });
    } else if (file.size === 0) {
      control.setErrors({ empty: true });
    } else if (file.size > MAX_FILE_SIZE) {
      control.setErrors({ maxSize: true });
    } else if (mode === 'decrypt' && !file.name.toUpperCase().endsWith('.ENV')) {
      control.setErrors({ envelopeExtension: true });
    } else {
      control.setErrors(null);
    }
    this.cdr.markForCheck();
  }

  removeFile(mode: EnvelopeMode, input: HTMLInputElement): void {
    this.formFor(mode).controls['file'].reset();
    input.value = '';
    this.result = null;
  }

  selectedFile(mode: EnvelopeMode): File | null {
    return this.formFor(mode).controls['file'].value as File | null;
  }

  fileError(mode: EnvelopeMode): string | null {
    const control = this.formFor(mode).controls['file'];
    if (!control.touched || !control.errors) {
      return null;
    }
    if (control.hasError('required')) return 'Selecciona un archivo.';
    if (control.hasError('empty')) return 'El archivo está vacío.';
    if (control.hasError('maxSize')) return 'El archivo supera el máximo de 50 MB.';
    if (control.hasError('envelopeExtension')) return 'El archivo para descifrar debe terminar en .ENV.';
    return 'El archivo seleccionado no es válido.';
  }

  submitEncrypt(): void {
    this.submit('encrypt');
  }

  submitDecrypt(): void {
    this.submit('decrypt');
  }

  retry(): void {
    if (this.operationError && !this.processingMode) {
      this.submit(this.lastAttemptMode);
    }
  }

  async downloadAgain(): Promise<void> {
    if (!this.lastResponse || !this.result) {
      return;
    }
    try {
      await this.downloads.save(this.lastResponse);
    } catch (error) {
      await this.handleError(error, 'No fue posible descargar nuevamente el resultado.');
    }
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} bytes`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  private buildForm(): FormGroup {
    return this.fb.group({
      clearingHouseId: [null as number | null, Validators.required],
      environment: ['Test', Validators.required],
      certificateVersionId: [null as number | null, Validators.required],
      file: [null as File | null, Validators.required]
    });
  }

  private formFor(mode: EnvelopeMode): FormGroup {
    return mode === 'encrypt' ? this.encryptForm : this.decryptForm;
  }

  private certificatesForMode(mode: EnvelopeMode): SobreDigitalCertificate[] {
    const requiredPurpose = mode === 'encrypt' ? 'OutboundEncryption' : 'InboundDecryption';
    return this.certificates.filter(certificate =>
      certificatePurposeCode(certificate.purpose) === requiredPurpose
      && (mode === 'encrypt' ? certificate.canEncrypt : certificate.canDecrypt));
  }

  private initializeContext(mode: EnvelopeMode): void {
    const candidates = this.certificatesForMode(mode)
      .sort((left, right) => right.versionNumber - left.versionNumber || right.id - left.id);
    const first = candidates[0];
    const form = this.formFor(mode);
    form.patchValue({
      clearingHouseId: first?.clearingHouseId ?? null,
      environment: first ? certificateEnvironmentCode(first.environment) : 'Test'
    }, { emitEvent: false });
    this.resolveCertificate(mode);
  }

  private registerAutomaticResolution(mode: EnvelopeMode): void {
    const form = this.formFor(mode);
    form.controls['clearingHouseId'].valueChanges.subscribe(() => this.resolveCertificate(mode));
    form.controls['environment'].valueChanges.subscribe(() => this.resolveCertificate(mode));
  }

  private resolveCertificate(mode: EnvelopeMode): void {
    const candidates = this.configurationCandidates(mode);
    this.formFor(mode).controls['certificateVersionId'].setValue(candidates[0]?.id ?? null, { emitEvent: false });
    this.cdr.markForCheck();
  }

  private submit(mode: EnvelopeMode): void {
    if (this.processingMode) {
      return;
    }
    const form = this.formFor(mode);
    form.markAllAsTouched();
    if (form.invalid) {
      return;
    }

    const file = form.controls['file'].value as File | null;
    const certificateVersionId = form.controls['certificateVersionId'].value as number | null;
    const certificate = this.selectedCertificate(mode);
    if (!file || !certificateVersionId || !certificate) {
      return;
    }

    this.operationError = null;
    this.result = null;
    this.lastResponse = null;
    this.lastAttemptMode = mode;
    this.processingMode = mode;
    this.cdr.markForCheck();

    const request = mode === 'encrypt'
      ? this.service.encrypt(file, certificateVersionId)
      : this.service.decrypt(file, certificateVersionId);

    request.pipe(finalize(() => {
      this.processingMode = null;
      this.cdr.markForCheck();
    })).subscribe({
      next: response => void this.completeOperation(mode, response, certificate),
      error: (error: HttpErrorResponse) => void this.handleError(
        error,
        mode === 'encrypt' ? 'No fue posible cifrar el archivo.' : 'No fue posible descifrar el archivo.'
      )
    });
  }

  private async completeOperation(
    mode: EnvelopeMode,
    response: HttpResponse<Blob>,
    certificate: SobreDigitalCertificate
  ): Promise<void> {
    try {
      const saved: SavedDownload = await this.downloads.save(response);
      this.lastResponse = response;
      this.result = {
        mode,
        fileName: saved.fileName,
        size: saved.size,
        completedAt: new Date(),
        certificate,
        cryptographicProfile: response.headers.get('x-cryptographic-profile')
      };
      this.notifications.success(
        mode === 'encrypt'
          ? `Sobre digital generado: ${saved.fileName}`
          : `Archivo descifrado correctamente: ${saved.fileName}`
      );
      this.formFor(mode).controls['file'].reset();
      this.cdr.markForCheck();
    } catch (error) {
      await this.handleError(error, 'No fue posible preparar la descarga.');
    }
  }

  private async handleError(error: unknown, fallback: string): Promise<void> {
    const parsed = await this.downloads.fromHttpError(error, fallback);
    this.operationError = presentNachaError(parsed, fallback);
    this.notifications.error(`${this.operationError.title}. ${this.operationError.message}`);
    this.cdr.markForCheck();
  }
}
