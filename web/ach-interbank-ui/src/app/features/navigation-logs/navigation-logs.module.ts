import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { NavigationLogsRoutingModule } from './navigation-logs-routing.module';
import { NavigationLogComponent } from './components/navigation-log.component';

@NgModule({
  imports: [SharedModule, NavigationLogsRoutingModule, NavigationLogComponent]
})
export class NavigationLogsModule {}
