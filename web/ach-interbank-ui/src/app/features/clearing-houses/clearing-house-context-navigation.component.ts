import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ClearingHouse } from './clearing-houses.models';

@Component({
  selector: 'app-clearing-house-context-navigation',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatTabsModule,
    MatTooltipModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-card class="context-card">
      <div class="context-card__identity">
        <a
          mat-stroked-button
          routerLink="/clearing-houses"
          aria-label="Volver a cámaras compensadoras"
          matTooltip="Volver al listado de cámaras"
        >
          <mat-icon>arrow_back</mat-icon>
          Volver a cámaras
        </a>
        <div>
          <span class="context-card__eyebrow">Cámara compensadora</span>
          <strong>{{ clearingHouse.name }}</strong>
          <span class="context-card__code">{{ clearingHouse.code }}</span>
        </div>
        <mat-chip [class.context-card__inactive]="!clearingHouse.isActive">
          {{ clearingHouse.isActive ? 'Activa' : 'Inactiva' }}
        </mat-chip>
      </div>

      <nav mat-tab-nav-bar [tabPanel]="panel" aria-label="Administración de la cámara">
        <a
          *ngIf="canReadPolicies"
          mat-tab-link
          [routerLink]="['/clearing-houses', clearingHouse.id, 'transaction-policies']"
          routerLinkActive
          #policiesActive="routerLinkActive"
          [active]="policiesActive.isActive"
          ariaCurrentWhenActive="page"
        >
          <mat-icon>policy</mat-icon>
          Políticas transaccionales
        </a>
        <a
          *ngIf="canReadCycles"
          mat-tab-link
          [routerLink]="['/clearing-houses', clearingHouse.id, 'cycles']"
          routerLinkActive
          #cyclesActive="routerLinkActive"
          [active]="cyclesActive.isActive"
          ariaCurrentWhenActive="page"
        >
          <mat-icon>schedule</mat-icon>
          Ciclos
        </a>
        <a
          *ngIf="canReadSpecialDates"
          mat-tab-link
          [routerLink]="['/clearing-houses', clearingHouse.id, 'special-dates']"
          routerLinkActive
          #specialDatesActive="routerLinkActive"
          [active]="specialDatesActive.isActive"
          ariaCurrentWhenActive="page"
        >
          <mat-icon>event</mat-icon>
          Fechas especiales
        </a>
      </nav>
      <mat-tab-nav-panel #panel></mat-tab-nav-panel>
    </mat-card>
  `,
  styleUrl: './clearing-house-context-navigation.component.scss'
})
export class ClearingHouseContextNavigationComponent {
  @Input({ required: true }) clearingHouse!: ClearingHouse;
  @Input() canReadPolicies = false;
  @Input() canReadCycles = false;
  @Input() canReadSpecialDates = false;
}
