import { CommonModule } from '@angular/common';
import { Component, Input, inject } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { BehaviorSubject, catchError, combineLatest, filter, map, of, startWith } from 'rxjs';
import { NavigationNamingService } from '../../../core/services/navigation-naming.service';

export interface MigaPan {
  etiqueta: string;
  ruta?: string;
}

@Component({
  selector: 'ui-migas-pan',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <nav aria-label="Ruta de navegación" class="migas" *ngIf="displayItems$ | async as displayItems">
      <ng-container *ngFor="let item of displayItems; let last = last">
        <a *ngIf="!last && item.ruta" [routerLink]="item.ruta">{{ item.etiqueta }}</a>
        <span *ngIf="last || !item.ruta">{{ item.etiqueta }}</span>
        <span *ngIf="!last">/</span>
      </ng-container>
    </nav>
  `,
  styles: [`.migas{display:flex;gap:.35rem;color:var(--color-text-soft);font-size:.9rem;margin-bottom:.75rem}.migas a{color:var(--color-primary);text-decoration:none}`]
})
export class UiMigasPanComponent {
  private readonly navigationNaming = inject(NavigationNamingService);
  private readonly router = inject(Router);
  private readonly itemsSubject = new BehaviorSubject<MigaPan[]>([]);

  readonly displayItems$ = combineLatest([
    this.itemsSubject,
    this.navigationNaming.menu$.pipe(
      startWith([]),
      catchError(() => of([]))
    ),
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(() => this.router.url),
      startWith(this.router.url)
    )
  ]).pipe(
    map(([items, menuItems, currentUrl]) => items.map((item, index) => ({
      ...item,
      etiqueta: this.navigationNaming.canonicalLabelForRoute(
        menuItems,
        item.ruta ?? (index === items.length - 1 ? currentUrl : undefined),
        item.etiqueta
      )
    })))
  );

  @Input()
  set items(value: MigaPan[] | null | undefined) {
    this.itemsSubject.next(value ?? []);
  }
}
