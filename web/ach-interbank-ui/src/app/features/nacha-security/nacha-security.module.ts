import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NachaSecurityRoutingModule } from './nacha-security-routing.module';
import { NachaCertificateManagerComponent } from './components/nacha-certificate-manager.component';
import { CertificateVersionsComponent } from './components/certificate-versions.component';
import { DigitalEnvelopeToolComponent } from './components/digital-envelope-tool.component';
import { NachaSecurityDashboardComponent } from './components/nacha-security-dashboard.component';
import { NachaGenerateOperationComponent } from './components/nacha-generate-operation.component';
import { NachaGenerateEncryptedOperationComponent } from './components/nacha-generate-encrypted-operation.component';
import { ManualEncryptOperationComponent } from './components/manual-encrypt-operation.component';
import { ManualDecryptOperationComponent } from './components/manual-decrypt-operation.component';

@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    NachaSecurityRoutingModule,
    NachaCertificateManagerComponent,
    CertificateVersionsComponent,
    DigitalEnvelopeToolComponent,
    NachaSecurityDashboardComponent,
    NachaGenerateOperationComponent,
    NachaGenerateEncryptedOperationComponent,
    ManualEncryptOperationComponent,
    ManualDecryptOperationComponent
  ]
})
export class NachaSecurityModule {}
