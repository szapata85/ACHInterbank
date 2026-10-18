import { Component, Input, TemplateRef } from '@angular/core';
import { PageHeaderComponent } from '../page-header.component';

/**
 * Compatibility adapter. New screens should use app-page-header directly.
 */
@Component({
  selector: 'ui-encabezado-pagina',
  standalone: true,
  imports: [PageHeaderComponent],
  template: `
    <app-page-header
      [title]="titulo"
      [description]="descripcion"
      [actionsTemplate]="acciones"
    ></app-page-header>
  `
})
export class UiEncabezadoPaginaComponent {
  @Input({ required: true }) titulo = '';
  @Input() descripcion = '';
  @Input() acciones?: TemplateRef<unknown> | null;
}
