import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import {
  NachaConfigProfileDetailReadModel,
  NachaConfigProfileFieldReadModel,
  NachaConfigProfileVariantReadModel
} from '../models/nacha-config-admin.models';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';

@Component({
  selector: 'app-nacha-config-profile-workspace-page',
  templateUrl: './nacha-config-profile-workspace-page.component.html',
  styleUrls: ['./nacha-config-profile-workspace-page.component.scss'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigProfileWorkspacePageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly query = inject(NachaConfigQueryService);
  private readonly cdr = inject(ChangeDetectorRef);

  perfilId = 0;
  perfil: NachaConfigProfileDetailReadModel | null = null;
  cargando = false;
  errorCarga = false;

  readonly columnasVariantes: ColDef<NachaConfigProfileVariantReadModel>[] = [
    { field: 'recordType', headerName: 'Record', minWidth: 90 },
    { field: 'variantCode', headerName: 'Variant', minWidth: 180 },
    { field: 'recordLength', headerName: 'Longitud', minWidth: 100 },
    { field: 'blockingFactor', headerName: 'Blocking factor', minWidth: 130 },
    { field: 'isActive', headerName: 'Activa', minWidth: 100 },
    { field: 'fieldCount', headerName: 'Fields', minWidth: 100 }
  ];

  readonly columnasFields: ColDef<NachaConfigProfileFieldReadModel>[] = [
    { field: 'recordType', headerName: 'Record', minWidth: 90 },
    { field: 'fieldName', headerName: 'Field', minWidth: 220 },
    { field: 'startPosition', headerName: 'Inicio', minWidth: 90 },
    { field: 'length', headerName: 'Longitud', minWidth: 100 },
    { field: 'endPosition', headerName: 'Fin', minWidth: 90 },
    { field: 'dataType', headerName: 'Tipo', minWidth: 120 },
    { field: 'sourceFieldPath', headerName: 'sourceFieldPath', minWidth: 220 },
    { field: 'paddingDirection', headerName: 'Padding', minWidth: 120 },
    { field: 'isComputed', headerName: 'Calculado', minWidth: 110 },
    { field: 'isControlTotalField', headerName: 'Control total', minWidth: 130 }
  ];

  ngOnInit(): void {
    this.perfilId = Number(this.route.snapshot.paramMap.get('id'));
    if (!this.perfilId) {
      this.router.navigate(['/nacha-config-admin/perfiles']);
      return;
    }

    this.cargar();
  }

  cargar(): void {
    this.cargando = true;
    this.errorCarga = false;

    this.query.detalleReadOnly(this.perfilId).pipe(finalize(() => {
      this.cargando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (perfil) => {
        this.perfil = perfil;
      },
      error: () => {
        this.errorCarga = true;
      }
    });
  }

  volver(): void {
    this.router.navigate(['/nacha-config-admin/perfiles']);
  }

  date(value?: string | null): string {
    return value ? new Date(value).toLocaleDateString('es-CO') : '-';
  }
}
