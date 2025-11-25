import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { CatalogsRoutingModule } from './catalogs-routing.module';
import { CatalogsListComponent } from './components/catalogs-list.component';

@NgModule({
  declarations: [CatalogsListComponent],
  imports: [SharedModule, CatalogsRoutingModule]
})
export class CatalogsModule {}
