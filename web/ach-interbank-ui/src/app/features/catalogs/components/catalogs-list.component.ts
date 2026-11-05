import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CatalogsApiService } from '../services/catalogs-api.service';
import { CatalogItem } from '../models/catalog.model';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-catalogs-list',
  templateUrl: './catalogs-list.component.html',
  styleUrls: ['./catalogs-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule, MatButtonModule, MatCardModule, MatIconModule]
})
export class CatalogsListComponent implements OnInit {
  private readonly api = inject(CatalogsApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly auth = inject(AuthService);
  readonly canManage = this.auth.hasPermission('CanManageAch');
  banks: CatalogItem[] = [];
  loading = false;
  loadError = false;
  readonly columnas: ColDef<CatalogItem>[] = [
    { field: 'code', headerName: 'Código', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'name', headerName: 'Nombre', sortable: true, filter: 'agTextColumnFilter' },
    { field: 'type', headerName: 'Tipo', sortable: true, filter: 'agTextColumnFilter' }
  ];

  readonly adminCatalogs = [
    { label: 'Entidades financieras', route: '/catalogs/financial-institutions', icon: 'account_balance' },
    { label: 'Prioridades por cámara', route: '/catalogs/clearing-house-preferences', icon: 'swap_horiz' },
    { label: 'Días no hábiles', route: '/catalogs/bank-holidays', icon: 'event_busy' },
    { label: 'Conceptos de lote', route: '/catalogs/company-entry-descriptions', icon: 'description' },
    { label: 'Tipos de documento', route: '/catalogs/document-types', icon: 'badge' },
    { label: 'Tipos de género', route: '/catalogs/gender-types', icon: 'people' },
    { label: 'Tipos de persona', route: '/catalogs/person-types', icon: 'person' },
    { label: 'Tipos de teléfono', route: '/catalogs/phone-types', icon: 'call' },
    { label: 'Tipos de correo', route: '/catalogs/email-types', icon: 'mail' },
    { label: 'Tipos de dirección', route: '/catalogs/address-types', icon: 'location_on' },
    { label: 'Códigos de transacción', route: '/catalogs/transaction-codes', icon: 'numbers' }
  ];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    if (this.loading) return;
    this.loading = true;
    this.loadError = false;
    this.api.listBanks().subscribe({
      next: banks => {
        this.banks = banks ?? [];
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.banks = [];
        this.loadError = true;
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }
}
