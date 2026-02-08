import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CatalogsApiService } from '../services/catalogs-api.service';
import { CatalogItem } from '../models/catalog.model';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-catalogs-list',
  templateUrl: './catalogs-list.component.html',
  styleUrls: ['./catalogs-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class CatalogsListComponent implements OnInit {
  private readonly api = inject(CatalogsApiService);
  banks: CatalogItem[] = [];
  readonly adminCatalogs = [
    { label: 'Tipos de documento', route: '/catalogs/document-types' },
    { label: 'Tipos de género', route: '/catalogs/gender-types' },
    { label: 'Tipos de persona', route: '/catalogs/person-types' },
    { label: 'Tipos de teléfono', route: '/catalogs/phone-types' },
    { label: 'Tipos de correo', route: '/catalogs/email-types' },
    { label: 'Tipos de dirección', route: '/catalogs/address-types' },
    { label: 'Códigos de transacción ACH', route: '/catalogs/transaction-codes' }
  ];

  ngOnInit(): void {
    this.api.listBanks().subscribe((banks) => (this.banks = banks));
  }
}
