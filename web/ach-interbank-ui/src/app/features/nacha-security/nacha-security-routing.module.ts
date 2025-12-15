import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NachaCertificateManagerComponent } from './components/nacha-certificate-manager.component';

const routes: Routes = [
  {
    path: 'certificates',
    component: NachaCertificateManagerComponent,
    data: {
      title: 'Seguridad NACHA',
      breadcrumb: 'Seguridad NACHA'
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NachaSecurityRoutingModule {}
