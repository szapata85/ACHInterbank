import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterModule } from '@angular/router';

export interface MigaPan {
  etiqueta: string;
  ruta?: string;
}

@Component({
  selector: 'ui-migas-pan',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <nav aria-label="Ruta de navegación" class="migas" *ngIf="items?.length">
      <ng-container *ngFor="let item of items; let last = last">
        <a *ngIf="!last && item.ruta" [routerLink]="item.ruta">{{ item.etiqueta }}</a>
        <span *ngIf="last || !item.ruta">{{ item.etiqueta }}</span>
        <span *ngIf="!last">/</span>
      </ng-container>
    </nav>
  `,
  styles: [`.migas{display:flex;gap:.35rem;color:var(--color-text-soft);font-size:.9rem;margin-bottom:.75rem}.migas a{color:var(--color-primary);text-decoration:none}`]
})
export class UiMigasPanComponent {
  @Input() items: MigaPan[] = [];
}
