import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CatalogsListComponent } from './components/catalogs-list.component';
import { permissionGuard } from '../../core/guards/permission.guard';

const routes: Routes = [
  {
    path: '',
    component: CatalogsListComponent,
    canActivate: [permissionGuard],
    data: { permissions: ['CanReadCatalogs'], breadcrumb: 'Catálogos', title: 'Catálogos' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CatalogsRoutingModule {}
