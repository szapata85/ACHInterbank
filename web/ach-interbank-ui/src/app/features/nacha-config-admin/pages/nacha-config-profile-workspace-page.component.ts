import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, finalize, map, of, switchMap } from 'rxjs';
import {
  NachaConfigFilterCatalogs,
  NachaConfigLayoutField,
  NachaConfigLayoutVariant,
  NachaConfigProfileRecord,
  NachaConfigWorkspace
} from '../../../core/models/nacha-config.model';
import { NachaConfigFieldListItem } from '../workspace/nacha-config-field-list.component';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';
import { nachaConfigUserMessage } from '../services/nacha-config-user-message';

@Component({
  selector: 'app-nacha-config-profile-workspace-page',
  templateUrl: './nacha-config-profile-workspace-page.component.html',
  styleUrls: ['./nacha-config-profile-workspace-page.component.scss'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigProfileWorkspacePageComponent implements OnInit {
  @ViewChild('workspaceHeading') private workspaceHeading?: ElementRef<HTMLElement>;
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly query = inject(NachaConfigQueryService);
  private readonly destroyRef = inject(DestroyRef);

  perfilId = 0;
  perfil: NachaConfigWorkspace | null = null;
  catalogos: NachaConfigFilterCatalogs | null = null;
  cargando = false;
  errorCarga = '';
  catalogosError = false;
  private focusedProfileId = 0;
  selectedRecordCode: string | null = null;
  selectedFieldId: number | null = null;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const nextProfileId = Number(params.get('id'));
        if (!Number.isFinite(nextProfileId) || nextProfileId <= 0) {
          void this.router.navigate(['/nacha-config-admin/perfiles']);
          return;
        }

        this.perfilId = nextProfileId;
        this.cargar();
      });
  }

  cargar(): void {
    this.cargando = true;
    this.errorCarga = '';
    this.catalogosError = false;

    this.query.workspace(this.perfilId).pipe(
      switchMap((perfil) => this.query.catalogosOperativos(perfil.camara, perfil.flujo).pipe(
        map((catalogos) => ({ perfil, catalogos, catalogosError: false })),
        catchError(() => of({ perfil, catalogos: null, catalogosError: true }))
      )),
      finalize(() => this.cargando = false)
    ).subscribe({
      next: ({ perfil, catalogos, catalogosError }) => {
        this.perfil = perfil;
        this.catalogos = catalogos;
        this.catalogosError = catalogosError;
        this.sincronizarSeleccion();
        this.enfocarTituloSiCorresponde(perfil.id);
      },
      error: (error: unknown) => {
        this.perfil = null;
        this.catalogos = null;
        this.errorCarga = nachaConfigUserMessage(
          error,
          'No fue posible cargar la configuración. Vuelve al listado o intenta nuevamente.'
        );
      }
    });
  }

  get records(): NachaConfigProfileRecord[] {
    return this.perfil?.records ?? [];
  }

  get selectedRecord(): NachaConfigProfileRecord | null {
    return this.records.find((record) => record.recordCode === this.selectedRecordCode) ?? null;
  }

  get fieldItems(): NachaConfigFieldListItem[] {
    if (!this.perfil || !this.selectedRecordCode) {
      return [];
    }

    return this.perfil.variantes
      .filter((variant) => variant.recordCode === this.selectedRecordCode)
      .flatMap((variant) => variant.fields.map((field) => ({ field, variant })))
      .sort((a, b) => a.field.startPosition - b.field.startPosition || a.field.fieldNameEs.localeCompare(b.field.fieldNameEs));
  }

  get selectedField(): NachaConfigLayoutField | null {
    return this.selectedFieldItem?.field ?? null;
  }

  get selectedVariant(): NachaConfigLayoutVariant | null {
    return this.selectedFieldItem?.variant ?? null;
  }

  seleccionarRegistro(recordCode: string): void {
    this.selectedRecordCode = recordCode;
    const firstField = this.fieldItems[0]?.field.id ?? null;
    this.selectedFieldId = firstField;
    this.actualizarContextoUrl();
  }

  seleccionarCampo(fieldId: number): void {
    this.selectedFieldId = fieldId;
    this.actualizarContextoUrl();
  }

  abrirPerfil(profileId: number): void {
    void this.router.navigate(['/nacha-config-admin/perfiles', profileId]);
  }

  volver(): void {
    void this.router.navigate(['/nacha-config-admin/perfiles']);
  }

  private get selectedFieldItem(): NachaConfigFieldListItem | null {
    return this.fieldItems.find((item) => item.field.id === this.selectedFieldId) ?? null;
  }

  private sincronizarSeleccion(): void {
    const requestedRecord = this.route.snapshot.queryParamMap.get('recordCode');
    const recordExists = requestedRecord && this.records.some((record) => record.recordCode === requestedRecord);
    this.selectedRecordCode = recordExists
      ? requestedRecord
      : [...this.records].sort((a, b) => a.sequence - b.sequence)[0]?.recordCode ?? null;

    const requestedFieldId = Number(this.route.snapshot.queryParamMap.get('fieldId'));
    const fieldExists = Number.isFinite(requestedFieldId) && this.fieldItems.some((item) => item.field.id === requestedFieldId);
    this.selectedFieldId = fieldExists ? requestedFieldId : this.fieldItems[0]?.field.id ?? null;
    this.actualizarContextoUrl();
  }

  private enfocarTituloSiCorresponde(profileId: number): void {
    if (this.focusedProfileId === profileId) {
      return;
    }
    this.focusedProfileId = profileId;
    requestAnimationFrame(() => this.workspaceHeading?.nativeElement.focus());
  }

  private actualizarContextoUrl(): void {
    if (!this.perfil) {
      return;
    }

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        recordCode: this.selectedRecordCode || null,
        fieldId: this.selectedFieldId || null
      },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }
}
