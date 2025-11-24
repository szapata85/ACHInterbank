import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';

import { ErrorMessageComponent } from './error-message.component';

@NgModule({
  imports: [CommonModule, ReactiveFormsModule, FormsModule, ErrorMessageComponent],
  exports: [CommonModule, ReactiveFormsModule, FormsModule, ErrorMessageComponent]
})
export class SharedModule {}
