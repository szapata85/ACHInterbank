import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SharedModule } from '../../../shared/shared.module';
import { BrandingService } from '../../../core/services/branding.service';
import { NotificationService } from '../../../core/services/notification.service';
import { BrandingSettings } from '../../../core/models/branding.model';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-branding-settings',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './branding-settings.component.html',
  styleUrls: ['./branding-settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BrandingSettingsComponent {
  private readonly brandingService = inject(BrandingService);
  private readonly notifications = inject(NotificationService);

  readonly branding$ = this.brandingService.branding$;

  private readonly defaultPublicBackground = '#0ea5e9';
  private readonly defaultPrivateBackground = '#f8fafc';
  private readonly defaultSidebarBackground = '#0f172a';
  private readonly defaultButtonColor = '#0ea5e9';

  publicLogoPreview: string | null | undefined = this.brandingService.getBrandingSnapshot().publicLogo;
  privateLogoPreview: string | null | undefined = this.brandingService.getBrandingSnapshot().privateLogo;
  publicBackground: string | null = this.brandingService.getBrandingSnapshot().publicBackground ?? null;
  privateBackground: string | null = this.brandingService.getBrandingSnapshot().privateBackground ?? null;
  sidebarBackground: string | null = this.brandingService.getBrandingSnapshot().sidebarBackground ?? null;
  buttonColor: string | null = this.brandingService.getBrandingSnapshot().buttonColor ?? null;
  isSaving = false;

  get publicBackgroundPreview(): string {
    return this.publicBackground ?? 'linear-gradient(135deg, #0ea5e9, #0f172a)';
  }

  get privateBackgroundPreview(): string {
    return this.privateBackground ?? this.defaultPrivateBackground;
  }

  get publicBackgroundInput(): string {
    return this.publicBackground ?? this.defaultPublicBackground;
  }

  get privateBackgroundInput(): string {
    return this.privateBackground ?? this.defaultPrivateBackground;
  }

  get sidebarBackgroundPreview(): string {
    return this.sidebarBackground ?? this.defaultSidebarBackground;
  }

  get sidebarBackgroundInput(): string {
    return this.sidebarBackground ?? this.defaultSidebarBackground;
  }

  get buttonColorInput(): string {
    return this.buttonColor ?? this.defaultButtonColor;
  }

  get buttonColorPreview(): string {
    return this.buttonColor ?? this.defaultButtonColor;
  }

  onFileSelected(event: Event, type: 'public' | 'private'): void {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];

    if (!file) {
      return;
    }

    if (!file.type.startsWith('image/')) {
      this.notifications.error('Solo se permiten archivos de imagen');
      target.value = '';
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      const value = reader.result as string;
      if (type === 'public') {
        this.publicLogoPreview = value;
      } else {
        this.privateLogoPreview = value;
      }
    };
    reader.readAsDataURL(file);
  }

  removeLogo(type: 'public' | 'private'): void {
    if (type === 'public') {
      this.publicLogoPreview = null;
    } else {
      this.privateLogoPreview = null;
    }
  }

  changeBackground(event: Event, type: 'public' | 'private' | 'sidebar'): void {
    const value = (event.target as HTMLInputElement).value;

    if (type === 'public') {
      this.publicBackground = value;
    } else if (type === 'private') {
      this.privateBackground = value;
    } else {
      this.sidebarBackground = value;
    }
  }

  clearBackground(type: 'public' | 'private' | 'sidebar'): void {
    switch (type) {
      case 'public':
        this.publicBackground = null;
        break;
      case 'private':
        this.privateBackground = null;
        break;
      case 'sidebar':
        this.sidebarBackground = null;
        break;
    }
  }

  changeButtonColor(event: Event): void {
    this.buttonColor = (event.target as HTMLInputElement).value;
  }

  clearButtonColor(): void {
    this.buttonColor = null;
  }

  reset(): void {
    const current = this.brandingService.getBrandingSnapshot();
    this.publicLogoPreview = current.publicLogo;
    this.privateLogoPreview = current.privateLogo;
    this.publicBackground = current.publicBackground ?? null;
    this.privateBackground = current.privateBackground ?? null;
    this.sidebarBackground = current.sidebarBackground ?? null;
    this.buttonColor = current.buttonColor ?? null;
  }

  save(): void {
    const payload: Partial<BrandingSettings> = {
      publicLogo: this.publicLogoPreview,
      privateLogo: this.privateLogoPreview,
      publicBackground: this.publicBackground,
      privateBackground: this.privateBackground,
      sidebarBackground: this.sidebarBackground,
      buttonColor: this.buttonColor
    };

    this.isSaving = true;
    this.brandingService
      .updateBranding(payload)
      .pipe(finalize(() => (this.isSaving = false)))
      .subscribe({
        next: () => this.notifications.success('Identidad actualizada'),
        error: () => this.notifications.error('No fue posible actualizar la identidad')
      });
  }
}
