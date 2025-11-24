import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CatalogsApiService } from '../services/catalogs-api.service';
import { CatalogItem } from '../models/catalog.model';

@Component({
  selector: 'app-catalogs-list',
  templateUrl: './catalogs-list.component.html',
  styleUrls: ['./catalogs-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CatalogsListComponent implements OnInit {
  private readonly api = inject(CatalogsApiService);
  banks: CatalogItem[] = [];

  ngOnInit(): void {
    this.api.listBanks().subscribe((banks) => (this.banks = banks));
  }
}
