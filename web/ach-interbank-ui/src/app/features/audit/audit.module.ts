import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { AuditRoutingModule } from './audit-routing.module';
import { AuditLogComponent } from './components/audit-log.component';

@NgModule({
  imports: [SharedModule, AuditRoutingModule, AuditLogComponent]
})
export class AuditModule {}
