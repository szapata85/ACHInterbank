import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';

import { ErrorMessageComponent } from './error-message.component';
import { TableComponent } from './components/table.component';
import { ConfirmDialogComponent } from './components/confirm-dialog.component';
import { PageHeaderComponent } from './components/page-header.component';
import { NotificationContainerComponent } from './components/notification-container.component';
import { CurrencyColPipe } from './pipes/currency-col.pipe';
import { DateFormatPipe } from './pipes/date-format.pipe';

@NgModule({
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    ErrorMessageComponent,
    TableComponent,
    ConfirmDialogComponent,
    PageHeaderComponent,
    NotificationContainerComponent
  ],
  declarations: [CurrencyColPipe, DateFormatPipe],
  exports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    ErrorMessageComponent,
    TableComponent,
    ConfirmDialogComponent,
    PageHeaderComponent,
    NotificationContainerComponent,
    CurrencyColPipe,
    DateFormatPipe
  ]
})
export class SharedModule {}
