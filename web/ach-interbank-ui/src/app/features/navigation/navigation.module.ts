import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { NavigationRoutingModule } from './navigation-routing.module';
import { NavigationMenuComponent } from './components/navigation-menu.component';

@NgModule({
  imports: [SharedModule, NavigationRoutingModule, NavigationMenuComponent]
})
export class NavigationModule {}
