import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MainLayoutComponent } from './main-layout.component';
import { LoginLayoutComponent } from './login-layout.component';

@NgModule({
  imports: [CommonModule, RouterModule, MainLayoutComponent, LoginLayoutComponent],
  exports: [MainLayoutComponent, LoginLayoutComponent]
})
export class LayoutModule {}
