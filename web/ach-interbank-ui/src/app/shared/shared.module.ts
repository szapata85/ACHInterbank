import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { ErrorMessageComponent } from './error-message.component';
import { TableComponent } from './components/table.component';
import { ConfirmDialogComponent } from './components/confirm-dialog.component';
import { PageHeaderComponent } from './components/page-header.component';
import { CurrencyColPipe } from './pipes/currency-col.pipe';
import { DateFormatPipe } from './pipes/date-format.pipe';
import { UnauthorizedComponent } from './components/status/unauthorized.component';
import { NotFoundComponent } from './components/status/not-found.component';
import { NotificationContainerComponent } from './components/notification-container.component';
import { LoadingOverlayComponent } from './components/loading-overlay.component';

@NgModule({
  declarations: [
    ErrorMessageComponent,
    TableComponent,
    ConfirmDialogComponent,
    PageHeaderComponent,
    UnauthorizedComponent,
    NotFoundComponent,
    CurrencyColPipe,
    DateFormatPipe,
    NotificationContainerComponent,
    LoadingOverlayComponent
  ],
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  exports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    RouterModule,
    ErrorMessageComponent,
    TableComponent,
    ConfirmDialogComponent,
    PageHeaderComponent,
    UnauthorizedComponent,
    NotFoundComponent,
    CurrencyColPipe,
    DateFormatPipe,
    NotificationContainerComponent,
    LoadingOverlayComponent
  ]
})
export class SharedModule {}
