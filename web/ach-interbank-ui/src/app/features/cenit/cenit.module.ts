import { NgModule } from '@angular/core';
import { CenitHomeComponent } from './components/cenit-home.component';
import { CenitOperationPageComponent } from './components/cenit-operation-page.component';
import { CenitRegulatoryPageComponent } from './components/cenit-regulatory-page.component';
import { CenitRoutingModule } from './cenit-routing.module';

@NgModule({
  imports: [CenitRoutingModule, CenitHomeComponent, CenitRegulatoryPageComponent, CenitOperationPageComponent]
})
export class CenitModule {}
