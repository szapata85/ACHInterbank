import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import {
  NachaOperationalAddendaRecord,
  NachaOperationalBatchControl,
  NachaOperationalBatchHeader,
  NachaOperationalEntryDetail,
  NachaOperationalFileControl,
  NachaOperationalFileDetail
} from '../models/nacha-operational.models';
import { NachaOperationalReadinessService } from '../services/nacha-operational-readiness.service';

@Component({
  selector: 'app-nacha-operational-file-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, SharedModule],
  templateUrl: './nacha-operational-file-detail.component.html',
  styleUrls: ['./nacha-operational-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaOperationalFileDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(NachaOperationalReadinessService);
  private readonly cdr = inject(ChangeDetectorRef);

  detail?: NachaOperationalFileDetail;
  cargando = false;
  error = '';

  readonly columnasBatches: ColDef<NachaOperationalBatchHeader>[] = [
    { field: 'batchNumber', headerName: 'Batch', minWidth: 110 },
    { field: 'serviceClassCode', headerName: 'Service class', minWidth: 140 },
    { field: 'companyName', headerName: 'Compania', minWidth: 180 },
    { field: 'standardEntryClassCode', headerName: 'SEC', minWidth: 100 },
    { field: 'companyEntryDescription', headerName: 'Descripcion', minWidth: 220 },
    { field: 'effectiveEntryDate', headerName: 'Vigencia', minWidth: 130 }
  ];

  readonly columnasEntries: ColDef<NachaOperationalEntryDetail>[] = [
    { field: 'transactionCode', headerName: 'Transaccion', minWidth: 130 },
    { field: 'receivingParticipantEntityCode', headerName: 'Entidad', minWidth: 130 },
    { field: 'checkDigit', headerName: 'Digito', minWidth: 90 },
    { field: 'accountNumberMasked', headerName: 'Cuenta', minWidth: 140 },
    { field: 'amount', headerName: 'Valor', minWidth: 130 },
    { field: 'recipIdNumberMasked', headerName: 'Documento', minWidth: 140 },
    { field: 'recipUserNameMasked', headerName: 'Nombre', minWidth: 140 },
    { field: 'sequenceNumberMasked', headerName: 'Secuencia', minWidth: 140 }
  ];

  readonly columnasAddendas: ColDef<NachaOperationalAddendaRecord>[] = [
    { field: 'codeTypeAddendumRecord', headerName: 'Tipo', minWidth: 100 },
    { field: 'businessType', headerName: 'Negocio', minWidth: 150 },
    { field: 'purposeOfTransaction', headerName: 'Proposito', minWidth: 180 },
    { field: 'invoiceOrAccountNumberMasked', headerName: 'Referencia', minWidth: 150 },
    { field: 'returnReasonCode', headerName: 'Causal', minWidth: 110 },
    { field: 'originalTraceNumberMasked', headerName: 'Trace original', minWidth: 150 },
    { field: 'newTraceNumberMasked', headerName: 'Trace nuevo', minWidth: 150 }
  ];

  readonly columnasBatchControls: ColDef<NachaOperationalBatchControl>[] = [
    { field: 'batchNumber', headerName: 'Batch', minWidth: 110 },
    { field: 'batchTranClassCode', headerName: 'Clase', minWidth: 110 },
    { field: 'entryAddendaCount', headerName: 'Entries/Addendas', minWidth: 160 },
    { field: 'entryHash', headerName: 'Entry hash', minWidth: 140 },
    { field: 'totalDebitAmount', headerName: 'Debitos', minWidth: 130 },
    { field: 'totalCreditAmount', headerName: 'Creditos', minWidth: 130 }
  ];

  readonly columnasFileControls: ColDef<NachaOperationalFileControl>[] = [
    { field: 'batchCount', headerName: 'Batches', minWidth: 110 },
    { field: 'blockCount', headerName: 'Bloques', minWidth: 110 },
    { field: 'entryAddendaCount', headerName: 'Entries/Addendas', minWidth: 160 },
    { field: 'entryHash', headerName: 'Entry hash', minWidth: 140 },
    { field: 'totalDebitAmount', headerName: 'Debitos', minWidth: 130 },
    { field: 'totalCreditAmount', headerName: 'Creditos', minWidth: 130 }
  ];

  ngOnInit(): void {
    const fileId = this.route.snapshot.paramMap.get('fileId') ?? '';
    this.cargar(fileId);
  }

  cargar(fileId: string): void {
    this.cargando = true;
    this.error = '';

    this.service.getFileDetail(fileId).pipe(finalize(() => {
      this.cargando = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (detail) => {
        this.detail = detail;
      },
      error: (err) => {
        this.error = err?.message ?? 'No fue posible cargar el detalle operativo NACHA-M.';
      }
    });
  }

  formatDate(value?: string | null): string {
    return value ? new Date(value).toLocaleString('es-CO') : '-';
  }
}
