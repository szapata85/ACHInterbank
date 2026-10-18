import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { NachaSecurityOperationResponse } from '../models/nacha-security-operation.model';
import { NachaSecurityOperationsApiService } from '../services/nacha-security-operations-api.service';
import { NachaOperationSummaryComponent } from './nacha-operation-summary.component';

@Component({
  selector: 'app-nacha-generate-operation',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    PageHeaderComponent,
    NachaOperationSummaryComponent
  ],
  templateUrl: './nacha-generate-operation.component.html',
  styleUrls: ['./nacha-operation-page.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaGenerateOperationComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(NachaSecurityOperationsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;
  operation?: NachaSecurityOperationResponse;

  readonly form = this.fb.group({
    cycleId: ['', Validators.required]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.operation = undefined;
    this.cdr.markForCheck();

    this.service.generatePlain({ cycleId: this.form.value.cycleId! })
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (response) => this.operation = response,
        error: () => this.notifications.error('No fue posible iniciar la generación NACHA-M.')
      });
  }
}
