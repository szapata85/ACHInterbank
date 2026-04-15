import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { CenitHomeComponent } from './components/cenit-home.component';
import { CenitOperationPageComponent } from './components/cenit-operation-page.component';
import { CenitRegulatoryPageComponent } from './components/cenit-regulatory-page.component';
import { CenitRoutingModule } from './cenit-routing.module';

@NgModule({
  declarations: [CenitHomeComponent, CenitRegulatoryPageComponent, CenitOperationPageComponent],
  imports: [CommonModule, FormsModule, RouterModule, SharedModule, CenitRoutingModule]
})
export class CenitModule {}
