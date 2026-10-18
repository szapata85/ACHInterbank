import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
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
    canActivate: [permissionGuard],
    data: {
      permissions: ['NavigationMenu.Manage', 'CanManageUsers'],
      breadcrumb: 'Organización del menú',
      title: 'Organización del menú'
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NavigationRoutingModule {}
