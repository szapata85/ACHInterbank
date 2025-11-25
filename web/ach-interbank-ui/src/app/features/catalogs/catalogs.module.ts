import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { CatalogsRoutingModule } from './catalogs-routing.module';
import { CatalogsListComponent } from './components/catalogs-list.component';

@NgModule({
  imports: [SharedModule, CatalogsRoutingModule, CatalogsListComponent]
})
export class CatalogsModule {}
