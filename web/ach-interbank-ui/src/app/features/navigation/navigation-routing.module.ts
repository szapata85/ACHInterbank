import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NavigationMenuComponent } from './components/navigation-menu.component';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'menu-items',
    pathMatch: 'full'
  },
  {
    path: 'menu-items',
    component: NavigationMenuComponent,
    data: { breadcrumb: 'Menús', title: 'Administrar menú de navegación' }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NavigationRoutingModule {}
