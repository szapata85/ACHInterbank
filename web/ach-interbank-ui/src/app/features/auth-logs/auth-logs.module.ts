import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { AuthLogsRoutingModule } from './auth-logs-routing.module';
import { AuthLogComponent } from './components/auth-log.component';

@NgModule({
  imports: [SharedModule, AuthLogsRoutingModule, AuthLogComponent]
})
export class AuthLogsModule {}
