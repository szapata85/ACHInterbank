import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { finalize } from 'rxjs';
import {
  NachaConfigEditorSourceTypeOption,
  NachaConfigFieldEditor,
  NachaConfigFieldRule
} from '../../../core/models/nacha-config.model';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { NachaConfigCommandService } from '../services/nacha-config-command.service';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { nachaConfigUserMessage } from '../services/nacha-config-user-message';

@Component({
  selector: 'app-nacha-config-field-editor',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatCardModule, MatExpansionModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule, MatSlideToggleModule
  ],
  templateUrl: './nacha-config-field-editor.component.html',
  styleUrls: ['./nacha-config-field-editor.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigFieldEditorComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);
  private readonly query = inject(NachaConfigQueryService);
  private readonly command = inject(NachaConfigCommandService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  @Input() profileId = 0;
  @Input() fieldId: number | null = null;
  @Input() profileEditable = false;
  @Output() saved = new EventEmitter<void>();

  editor: NachaConfigFieldEditor | null = null;
  loading = false;
  saving = false;
  loadError = '';
  saveError = '';
  ruleDefinitions: NachaConfigFieldRule[] = [];
  private requestSequence = 0;
  readonly canManage = this.auth.hasPermission(['Config.Manage', 'CanManageAch']);

  readonly form = this.fb.group({
    fieldNameEs: ['', [Validators.required, Validators.maxLength(150)]],
    startPosition: [1, [Validators.required, Validators.min(1)]],
    length: [1, [Validators.required, Validators.min(1)]],
    isEnabled: [true],
    padChar: [' ', [Validators.required, Validators.maxLength(1), Validators.minLength(1)]],
    justification: ['', Validators.required],
    formatMask: [''],
    sourceTypeCode: ['', Validators.required],
    sourceDefinitionId: [null as number | null],
    fixedValue: [''],
    rules: this.fb.array<FormGroup<{
      id: FormControl<number | null>;
      errorMessageEs: FormControl<string | null>;
      severity: FormControl<string | null>;
      isEnabled: FormControl<boolean | null>;
    }>>([])
  });

  get rules(): FormArray {
    return this.form.controls.rules;
  }

  get canEdit(): boolean {
    return this.canManage && this.profileEditable && !!this.editor?.isEditable;
  }

  get selectedSourceType(): NachaConfigEditorSourceTypeOption | null {
    const code = this.form.controls.sourceTypeCode.value;
    return this.editor?.sourceTypes.find((item) => item.code === code) ?? null;
  }

  get sourceOptions() {
    const code = this.form.controls.sourceTypeCode.value;
    return (this.editor?.sourceOptions ?? []).filter((item) => item.sourceTypeCode === code);
  }

  ngOnChanges(): void {
    this.loadEditor();
  }

  sourceTypeChanged(): void {
    const selected = this.selectedSourceType;
    if (selected?.usesFixedValue) {
      this.form.controls.sourceDefinitionId.setValue(null);
    } else {
      this.form.controls.fixedValue.setValue('');
      const selectedDefinition = this.form.controls.sourceDefinitionId.value;
      if (selectedDefinition && !this.sourceOptions.some((item) => item.sourceDefinitionId === selectedDefinition)) {
        this.form.controls.sourceDefinitionId.setValue(null);
      }
    }
  }

  save(): void {
    if (!this.canEdit || this.saving || !this.editor) {
      return;
    }
    const sourceType = this.selectedSourceType;
    if (this.form.invalid || (sourceType?.requiresDefinitionSelection && !this.form.controls.sourceDefinitionId.value)) {
      this.form.markAllAsTouched();
      this.saveError = sourceType?.requiresDefinitionSelection && !this.form.controls.sourceDefinitionId.value
        ? 'Selecciona el dato o cálculo que alimentará el campo.'
        : 'Revisa los datos obligatorios antes de guardar.';
      return;
    }

    const value = this.form.getRawValue();
    this.saving = true;
    this.saveError = '';
    this.command.actualizarEditorCampo(this.profileId, this.editor.fieldId, {
      fieldNameEs: (value.fieldNameEs ?? '').trim(),
      startPosition: Number(value.startPosition),
      length: Number(value.length),
      padChar: value.padChar ?? ' ',
      justification: value.justification ?? '',
      formatMask: (value.formatMask ?? '').trim() || null,
      isEnabled: !!value.isEnabled,
      source: {
        sourceTypeCode: value.sourceTypeCode ?? '',
        sourceDefinitionId: value.sourceDefinitionId,
        fixedValue: sourceType?.usesFixedValue ? value.fixedValue ?? '' : null
      },
      rules: (value.rules ?? []).map((rule) => ({
        id: Number(rule.id),
        errorMessageEs: String(rule.errorMessageEs ?? '').trim(),
        severity: String(rule.severity ?? ''),
        isEnabled: !!rule.isEnabled
      })),
      expectedRowVersion: this.editor.rowVersion
    }).pipe(finalize(() => {
      this.saving = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: () => {
        this.notifications.success('Campo actualizado correctamente.');
        this.saved.emit();
      },
      error: (error) => {
        this.saveError = this.errorMessage(error);
        this.notifications.error(this.saveError);
        this.cdr.markForCheck();
      }
    });
  }

  reset(): void {
    if (this.editor) {
      this.applyEditor(this.editor);
    }
  }

  private loadEditor(): void {
    const sequence = ++this.requestSequence;
    if (!this.profileId || !this.fieldId) {
      this.editor = null;
      this.loading = false;
      this.loadError = '';
      this.form.disable({ emitEvent: false });
      return;
    }

    this.loading = true;
    this.loadError = '';
    this.saveError = '';
    this.query.editorCampo(this.profileId, this.fieldId).pipe(finalize(() => {
      if (sequence === this.requestSequence) {
        this.loading = false;
        this.cdr.markForCheck();
      }
    })).subscribe({
      next: (editor) => {
        if (sequence !== this.requestSequence) return;
        this.editor = editor;
        this.applyEditor(editor);
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        if (sequence !== this.requestSequence) return;
        this.editor = null;
        this.loadError = nachaConfigUserMessage(
          error,
          'No fue posible cargar las opciones del campo. Selecciónalo nuevamente o recarga el perfil.'
        );
        this.form.disable({ emitEvent: false });
        this.cdr.markForCheck();
      }
    });
  }

  private applyEditor(editor: NachaConfigFieldEditor): void {
    const field = editor.field;
    const orderedRules = [...field.reglas].sort((a, b) => (a.order ?? 0) - (b.order ?? 0) || a.id - b.id);
    this.ruleDefinitions = orderedRules;
    this.rules.clear({ emitEvent: false });
    orderedRules.forEach((rule) => this.rules.push(this.ruleGroup(rule), { emitEvent: false }));
    this.form.reset({
      fieldNameEs: field.fieldNameEs,
      startPosition: field.startPosition,
      length: field.length,
      isEnabled: field.isEnabled,
      padChar: field.padChar ?? ' ',
      justification: field.justification ?? '',
      formatMask: field.formatMask ?? '',
      sourceTypeCode: field.sourceType ?? '',
      sourceDefinitionId: field.sourceType && editor.sourceTypes.find((item) => item.code === field.sourceType)?.requiresDefinitionSelection
        ? field.sourceDefinitionId ?? null
        : null,
      fixedValue: field.constantValue ?? '',
      rules: orderedRules.map((rule) => ({
        id: rule.id,
        errorMessageEs: rule.errorMessageEs,
        severity: rule.severity,
        isEnabled: rule.isEnabled
      }))
    }, { emitEvent: false });
    if (this.canEdit) this.form.enable({ emitEvent: false }); else this.form.disable({ emitEvent: false });
  }

  private ruleGroup(rule: NachaConfigFieldRule) {
    return this.fb.group({
      id: [rule.id, Validators.required],
      errorMessageEs: [rule.errorMessageEs, [Validators.required, Validators.maxLength(500)]],
      severity: [rule.severity, Validators.required],
      isEnabled: [rule.isEnabled]
    });
  }

  private errorMessage(error: unknown): string {
    return nachaConfigUserMessage(
      error,
      'No fue posible guardar el campo. La configuración se conserva sin cambios; revisa los datos e intenta nuevamente.'
    );
  }
}
