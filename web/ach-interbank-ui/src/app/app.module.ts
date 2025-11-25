import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { CoreModule } from './core/core.module';
import { SharedModule } from './shared/shared.module';
import { LayoutModule } from './layout/layout.module';
import { NotificationContainerComponent } from './shared/components/notification-container.component';
import { LoadingOverlayComponent } from './shared/components/loading-overlay.component';

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    RouterModule,
    CoreModule,
    SharedModule,
    LayoutModule,
    NotificationContainerComponent,
    LoadingOverlayComponent,
    AppRoutingModule
  ],
  bootstrap: [AppComponent]
})
export class AppModule {}
