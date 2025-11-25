import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MainLayoutComponent } from './main-layout.component';
import { LoginLayoutComponent } from './login-layout.component';
import { SharedModule } from '../shared/shared.module';

@NgModule({
  declarations: [MainLayoutComponent, LoginLayoutComponent],
  imports: [CommonModule, RouterModule, SharedModule],
  exports: [MainLayoutComponent, LoginLayoutComponent]
})
export class LayoutModule {}
