import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { AgGridAngular } from 'ag-grid-angular';
import {
  ColDef,
  GridApi,
  GridOptions,
  GridReadyEvent,
  IServerSideDatasource,
  RowModelType,
  RowSelectionOptions
} from 'ag-grid-community';
import { UiCampoBusquedaComponent } from './ui-campo-busqueda.component';
import { UiEstadoCargaComponent } from './ui-estado-carga.component';
import { UiEstadoErrorComponent } from './ui-estado-error.component';
import { UiEstadoVacioComponent } from './ui-estado-vacio.component';

@Component({
  selector: 'ui-grilla-empresarial',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AgGridAngular, UiCampoBusquedaComponent, UiEstadoCargaComponent, UiEstadoErrorComponent, UiEstadoVacioComponent],
  template: `
    <section class="grilla-empresarial">
      <header class="toolbar">
        <ui-campo-busqueda [control]="busquedaControl" [placeholder]="placeholderBusqueda" (buscar)="aplicarBusquedaGlobal()"></ui-campo-busqueda>
        <ng-content select="[acciones-toolbar]"></ng-content>
      </header>

      <div class="estado-contenedor" [class.visible]="cargando">
        <ui-estado-carga *ngIf="cargando" [mensaje]="mensajeCargando"></ui-estado-carga>
      </div>
      <div class="estado-contenedor" [class.visible]="error">
        <ui-estado-error *ngIf="error" [mensaje]="mensajeError" (reintentar)="reintentar.emit()"></ui-estado-error>
      </div>
      <div class="estado-contenedor" [class.visible]="!cargando && !error && totalFilas === 0">
        <ui-estado-vacio *ngIf="!cargando && !error && totalFilas === 0" [titulo]="tituloVacio" [mensaje]="mensajeVacio"></ui-estado-vacio>
      </div>

      <ag-grid-angular
        *ngIf="!error"
        class="ag-theme-alpine grilla"
        [columnDefs]="columnas"
        [defaultColDef]="columnaPorDefecto"
        [rowData]="datos"
        [rowSelection]="seleccionFilas"
        [rowModelType]="modoFila"
        [gridOptions]="gridOptions"
        [pagination]="paginacion"
        [paginationPageSize]="tamanoPagina"
        [paginationPageSizeSelector]="selectorTamanoPagina"
        [suppressNoRowsOverlay]="true"
        [loading]="cargando"
        (gridReady)="onGridReady($event)"
        (selectionChanged)="onSelectionChanged()"
      ></ag-grid-angular>
    </section>
  `,
  styles: [
    `
      :host { display: block; min-width: 0; max-width: 100%; }
      .grilla-empresarial { display: grid; gap: .75rem; min-width: 0; max-width: 100%; }
      .toolbar { display: flex; justify-content: space-between; align-items: center; gap: .75rem; flex-wrap: wrap; }
      .grilla { width: 100%; max-width: 100%; min-width: 0; min-height: 320px; border: 1px solid var(--color-border); border-radius: var(--radius-lg); overflow: hidden; transition: opacity .18s ease; }
      .estado-contenedor { opacity: 0; transform: translateY(2px); transition: opacity .2s ease, transform .2s ease; pointer-events: none; height: 0; overflow: hidden; }
      .estado-contenedor.visible { opacity: 1; transform: translateY(0); pointer-events: auto; height: auto; }
      :host ::ng-deep .ag-header-cell-label { font-weight: 700; }
      :host ::ng-deep .ag-row { transition: background-color .18s ease; }
      :host ::ng-deep .ag-row:hover { background: #f8fbff !important; }
      :host ::ng-deep .ag-row.ag-row-selected { background: #e8f0ff !important; }
      :host ::ng-deep .ag-cell .btn-grid { font-size: .78rem; padding: .25rem .5rem; border-radius: var(--radius-sm); }
      @media (max-width: 600px) {
        .toolbar { align-items: stretch; flex-direction: column; }
        :host ::ng-deep .ag-paging-panel { flex-wrap: wrap; height: auto; min-height: 56px; padding-block: .35rem; }
        :host ::ng-deep .ag-paging-page-summary-panel { margin-left: 0; }
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiGrillaEmpresarialComponent<TData = any> {
  @Input() columnas: ColDef<TData>[] = [];
  @Input() datos: TData[] = [];
  @Input() cargando = false;
  @Input() error = false;
  @Input() mensajeError = 'No fue posible cargar la grilla.';
  @Input() mensajeCargando = 'Cargando información...';
  @Input() tituloVacio = 'Sin resultados';
  @Input() mensajeVacio = 'No hay registros para mostrar.';
  @Input() placeholderBusqueda = 'Buscar en la grilla...';
  @Input() paginacion = true;
  @Input() tamanoPagina = 20;
  @Input() seleccionFilas: RowSelectionOptions = { mode: 'singleRow' };
  @Input() modoFila: RowModelType = 'clientSide';
  @Input() datasourceServidor?: IServerSideDatasource;

  @Output() seleccionCambio = new EventEmitter<TData[]>();
  @Output() grillaLista = new EventEmitter<GridApi<TData>>();
  @Output() reintentar = new EventEmitter<void>();

  readonly busquedaControl = new FormControl<string>('');

  readonly columnaPorDefecto: ColDef<TData> = {
    sortable: true,
    resizable: true,
    filter: true,
    floatingFilter: true,
    tooltipValueGetter: (params) => (params.value == null ? '' : String(params.value))
  };

  readonly gridOptions: GridOptions<TData> = {
    animateRows: true,
    suppressCellFocus: true,
    localeText: {
      noRowsToShow: 'No hay filas para mostrar',
      page: 'Página',
      pageSizeSelectorLabel: 'Filas por página',
      to: 'a',
      of: 'de',
      next: 'Siguiente',
      last: 'Última',
      first: 'Primera',
      previous: 'Anterior',
      loadingOoo: 'Cargando...'
    }
  };

  private api?: GridApi<TData>;
  private readonly selectorTamanoPaginaBase = [10, 20, 50, 100];

  get selectorTamanoPagina(): number[] {
    return Array.from(new Set<number>([...this.selectorTamanoPaginaBase, this.tamanoPagina])).sort((a, b) => a - b);
  }

  get totalFilas(): number {
    return this.datos?.length ?? 0;
  }

  onGridReady(event: GridReadyEvent<TData>): void {
    this.api = event.api;

    if (this.modoFila === 'serverSide' && this.datasourceServidor) {
      this.api.setGridOption('serverSideDatasource', this.datasourceServidor);
    }

    this.grillaLista.emit(event.api);
  }

  aplicarBusquedaGlobal(): void {
    this.api?.setGridOption('quickFilterText', this.busquedaControl.value ?? '');
  }

  onSelectionChanged(): void {
    const seleccion = this.api?.getSelectedRows() ?? [];
    this.seleccionCambio.emit(seleccion);
  }
}
