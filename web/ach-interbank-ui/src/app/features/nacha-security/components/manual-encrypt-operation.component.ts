import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { NachaSecurityOperationResponse } from '../models/nacha-security-operation.model';
import { NachaSecurityOperationsApiService } from '../services/nacha-security-operations-api.service';
import { NachaOperationSummaryComponent } from './nacha-operation-summary.component';

@Component({
  selector: 'app-manual-encrypt-operation',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    PageHeaderComponent,
    NachaOperationSummaryComponent
  ],
  templateUrl: './manual-encrypt-operation.component.html',
  styleUrls: ['./nacha-operation-page.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManualEncryptOperationComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(NachaSecurityOperationsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;
  operation?: NachaSecurityOperationResponse;

  readonly form = this.fb.group({
    file: [null as File | null, Validators.required]
  });

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.form.patchValue({ file: input.files?.[0] ?? null });
    this.form.controls.file.markAsTouched();
  }

  submit(): void {
    const file = this.form.value.file;
    if (!file) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.operation = undefined;
    this.service.manualEncrypt(file)
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (response) => {
          this.operation = response;
          this.cdr.markForCheck();
        },
        error: () => this.notifications.error('No fue posible cifrar el archivo.')
      });
  }
}
