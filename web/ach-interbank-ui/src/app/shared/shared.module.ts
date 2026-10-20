import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';

import { ErrorMessageComponent } from './error-message.component';
import { TableComponent } from './components/table.component';
import { ConfirmDialogComponent } from './components/confirm-dialog.component';
import { PageHeaderComponent } from './components/page-header.component';
import { CurrencyColPipe } from './pipes/currency-col.pipe';
import { DateFormatPipe } from './pipes/date-format.pipe';
import { UnauthorizedComponent } from './components/status/unauthorized.component';
import { NotFoundComponent } from './components/status/not-found.component';
import { ClearingHouseSelectComponent } from './components/clearing-house-select.component';
import { UiBotonComponent } from './components/ui/ui-boton.component';
import { UiCampoTextoComponent } from './components/ui/ui-campo-texto.component';
import { UiCampoNumeroComponent } from './components/ui/ui-campo-numero.component';
import { UiCampoMonedaComponent } from './components/ui/ui-campo-moneda.component';
import { UiCampoFechaComponent } from './components/ui/ui-campo-fecha.component';
import { UiCampoTextareaComponent } from './components/ui/ui-campo-textarea.component';
import { UiCampoBusquedaComponent } from './components/ui/ui-campo-busqueda.component';
import { UiSelectorBuscableComponent } from './components/ui/ui-selector-buscable.component';
import { UiEstadoCargaComponent } from './components/ui/ui-estado-carga.component';
import { UiEstadoVacioComponent } from './components/ui/ui-estado-vacio.component';
import { UiEstadoErrorComponent } from './components/ui/ui-estado-error.component';
import { UiEncabezadoPaginaComponent } from './components/ui/ui-encabezado-pagina.component';
import { UiMigasPanComponent } from './components/ui/ui-migas-pan.component';
import { UiBarraFiltrosComponent } from './components/ui/ui-barra-filtros.component';
import { UiTarjetaComponent } from './components/ui/ui-tarjeta.component';
import { UiAlertaComponent } from './components/ui/ui-alerta.component';
import { UiEtiquetaEstadoComponent } from './components/ui/ui-etiqueta-estado.component';
import { UiModalConfirmacionComponent } from './components/ui/ui-modal-confirmacion.component';
import { UiGrillaEmpresarialComponent } from './components/ui/ui-grilla-empresarial.component';
import { UiFormularioSeccionComponent } from './components/ui/ui-formulario-seccion.component';
import { UiFormularioAccionesComponent } from './components/ui/ui-formulario-acciones.component';
import { UiErrorCampoComponent } from './forms/ui-error-campo.component';
import { AccionProtegidaDirective } from './directives/accion-protegida.directive';
import { UiIconComponent } from './components/ui/ui-icon.component';
import { OperationalErrorPanelComponent } from './components/operational-error-panel.component';

@NgModule({
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    ErrorMessageComponent,
    TableComponent,
    ConfirmDialogComponent,
    PageHeaderComponent,
    UnauthorizedComponent,
    NotFoundComponent,
    ClearingHouseSelectComponent,
    CurrencyColPipe,
    DateFormatPipe,
    UiBotonComponent,
    UiCampoTextoComponent,
    UiCampoNumeroComponent,
    UiCampoMonedaComponent,
    UiCampoFechaComponent,
    UiCampoTextareaComponent,
    UiCampoBusquedaComponent,
    UiSelectorBuscableComponent,
    UiEstadoCargaComponent,
    UiEstadoVacioComponent,
    UiEstadoErrorComponent,
    UiEncabezadoPaginaComponent,
    UiMigasPanComponent,
    UiBarraFiltrosComponent,
    UiTarjetaComponent,
    UiAlertaComponent,
    UiEtiquetaEstadoComponent,
    UiModalConfirmacionComponent,
    UiGrillaEmpresarialComponent,
    UiFormularioSeccionComponent,
    UiFormularioAccionesComponent,
    UiErrorCampoComponent,
    AccionProtegidaDirective,
    UiIconComponent,
    OperationalErrorPanelComponent
  ],
  exports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    ErrorMessageComponent,
    TableComponent,
    ConfirmDialogComponent,
    PageHeaderComponent,
    UnauthorizedComponent,
    NotFoundComponent,
    ClearingHouseSelectComponent,
    CurrencyColPipe,
    DateFormatPipe,
    UiBotonComponent,
    UiCampoTextoComponent,
    UiCampoNumeroComponent,
    UiCampoMonedaComponent,
    UiCampoFechaComponent,
    UiCampoTextareaComponent,
    UiCampoBusquedaComponent,
    UiSelectorBuscableComponent,
    UiEstadoCargaComponent,
    UiEstadoVacioComponent,
    UiEstadoErrorComponent,
    UiEncabezadoPaginaComponent,
    UiMigasPanComponent,
    UiBarraFiltrosComponent,
    UiTarjetaComponent,
    UiAlertaComponent,
    UiEtiquetaEstadoComponent,
    UiModalConfirmacionComponent,
    UiGrillaEmpresarialComponent,
    UiFormularioSeccionComponent,
    UiFormularioAccionesComponent,
    UiErrorCampoComponent,
    AccionProtegidaDirective,
    UiIconComponent,
    OperationalErrorPanelComponent
  ]
})
export class SharedModule {}
