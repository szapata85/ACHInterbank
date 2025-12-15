import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NachaSecurityRoutingModule } from './nacha-security-routing.module';
import { NachaCertificateManagerComponent } from './components/nacha-certificate-manager.component';

@NgModule({
  declarations: [],
  imports: [CommonModule, NachaSecurityRoutingModule, NachaCertificateManagerComponent]
})
export class NachaSecurityModule {}
