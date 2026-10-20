import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs';
import {
  NachaConfigFieldPreview,
  NachaConfigOperationalIssue,
  NachaConfigOperationalTestResult,
  NachaConfigRecordPreview
} from '../../../core/models/nacha-config.model';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { nachaConfigUserMessage } from '../services/nacha-config-user-message';

@Component({
  selector: 'app-nacha-config-test-panel',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatExpansionModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './nacha-config-test-panel.component.html',
  styleUrls: ['./nacha-config-test-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigTestPanelComponent {
  private readonly command = inject(NachaConfigCommandService);
  private readonly cdr = inject(ChangeDetectorRef);

  @Input() profileId = 0;
  @Input() selectedRecordCode: string | null = null;
  @Input() selectedFieldId: number | null = null;
  @Output() selectedRecordCodeChange = new EventEmitter<string>();
  @Output() selectedFieldIdChange = new EventEmitter<number>();

  result: NachaConfigOperationalTestResult | null = null;
  loading = false;
  errorMessage = '';
  selectedVariantId: number | null = null;

  probar(): void {
    if (!this.profileId || this.loading) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.command.probar(this.profileId).pipe(finalize(() => {
      this.loading = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (result) => {
        this.result = result;
        this.syncSelection();
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        this.result = null;
        this.errorMessage = nachaConfigUserMessage(
          error,
          'No fue posible probar la configuración. La configuración se conserva sin cambios.'
        );
        this.cdr.markForCheck();
      }
    });
  }

  get recordOptions(): Array<{ code: string; name: string }> {
    const seen = new Set<string>();
    const options: Array<{ code: string; name: string }> = [];
    for (const record of this.result?.records ?? []) {
      if (!seen.has(record.recordCode)) {
        seen.add(record.recordCode);
        options.push({ code: record.recordCode, name: record.recordNameEs });
      }
    }
    return options;
  }

  get variantsForSelectedRecord(): NachaConfigRecordPreview[] {
    if (!this.result || !this.selectedRecordCode) {
      return [];
    }
    return this.result.records.filter((record) => record.recordCode === this.selectedRecordCode);
  }

  get selectedPreview(): NachaConfigRecordPreview | null {
    const variants = this.variantsForSelectedRecord;
    return variants.find((variant) => variant.variantId === this.selectedVariantId)
      ?? variants.find((variant) => variant.isDefaultForRecord)
      ?? variants[0]
      ?? null;
  }

  get selectedField(): NachaConfigFieldPreview | null {
    const fields = this.selectedPreview?.fields ?? [];
    return fields.find((field) => field.fieldId === this.selectedFieldId) ?? fields[0] ?? null;
  }

  seleccionarRegistro(code: string): void {
    this.selectedRecordCodeChange.emit(code);
    const variants = (this.result?.records ?? []).filter((record) => record.recordCode === code);
    const variant = variants.find((item) => item.isDefaultForRecord) ?? variants[0] ?? null;
    this.selectedVariantId = variant?.variantId ?? null;
    const fieldId = variant?.fields[0]?.fieldId;
    if (fieldId) {
      this.selectedFieldIdChange.emit(fieldId);
    }
  }

  seleccionarVariante(variant: NachaConfigRecordPreview): void {
    this.selectedVariantId = variant.variantId;
    const currentExists = variant.fields.some((field) => field.fieldId === this.selectedFieldId);
    if (!currentExists && variant.fields[0]) {
      this.selectedFieldIdChange.emit(variant.fields[0].fieldId);
    }
  }

  seleccionarCampo(fieldId: number): void {
    this.selectedFieldIdChange.emit(fieldId);
  }

  gridColumn(field: NachaConfigFieldPreview): string {
    const total = Math.max(1, this.selectedPreview?.totalLength ?? 1);
    const start = Math.max(1, field.startPosition);
    const endExclusive = Math.min(total + 1, field.endPosition + 1);
    return `${start} / ${Math.max(start + 1, endExclusive)}`;
  }


  fieldAria(field: NachaConfigFieldPreview): string {
    const value = field.sampleValue === null || field.sampleValue === undefined ? 'sin valor de ejemplo' : 'con valor de ejemplo';
    return `${field.fieldNameEs}, posiciones ${field.startPosition} a ${field.endPosition}, ${value}`;
  }

  issueSymbol(issue: NachaConfigOperationalIssue): string {
    return issue.severity.toUpperCase() === 'WARN' ? '⚠' : '✕';
  }

  private syncSelection(): void {
    const records = this.result?.records ?? [];
    if (records.length === 0) {
      this.selectedVariantId = null;
      return;
    }

    const recordCode = this.selectedRecordCode && records.some((record) => record.recordCode === this.selectedRecordCode)
      ? this.selectedRecordCode
      : records[0].recordCode;
    if (recordCode !== this.selectedRecordCode) {
      this.selectedRecordCodeChange.emit(recordCode);
    }

    const variants = records.filter((record) => record.recordCode === recordCode);
    const variant = variants.find((item) => item.isDefaultForRecord) ?? variants[0];
    this.selectedVariantId = variant?.variantId ?? null;
    if (variant && !variant.fields.some((field) => field.fieldId === this.selectedFieldId) && variant.fields[0]) {
      this.selectedFieldIdChange.emit(variant.fields[0].fieldId);
    }
  }
}
