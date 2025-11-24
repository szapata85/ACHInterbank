import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

import { AppShellComponent } from './app-shell.component';

@NgModule({
  declarations: [AppShellComponent],
  imports: [CommonModule, RouterModule],
  exports: [AppShellComponent]
})
export class LayoutModule {}
