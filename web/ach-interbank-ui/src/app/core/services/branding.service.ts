import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, of, tap } from 'rxjs';
import {
  BrandingSettings,
  DEFAULT_BRANDING_SETTINGS,
  DEFAULT_PRODUCT_NAME
} from '../models/branding.model';
import { ApiService } from './api.service';

const RUNTIME_TOKENS = {
  primary: '--brand-primary',
  onPrimary: '--brand-on-primary',
  primaryHover: '--brand-primary-hover',
  publicBackground: '--brand-public-background',
  privateBackground: '--brand-private-background',
  sidebarBackground: '--brand-sidebar-background',
  materialPrimary: '--mat-sys-primary',
  materialOnPrimary: '--mat-sys-on-primary',
  sidebarOnBackground: '--brand-sidebar-on-background',
  legacyButtonColor: '--button-color'
} as const;

@Injectable({ providedIn: 'root' })
export class BrandingService {
  private readonly api = inject(ApiService);
  private readonly brandingSubject = new BehaviorSubject<BrandingSettings>({});
  readonly branding$: Observable<BrandingSettings> = this.brandingSubject.asObservable();
  readonly productName = DEFAULT_PRODUCT_NAME;

  constructor() {
    this.refreshFromServer().subscribe();
  }

  updateBranding(settings: Partial<BrandingSettings>): Observable<BrandingSettings> {
    return this.api
      .put<BrandingSettings>('api/users/branding', settings)
      .pipe(
        map((response) => response ?? {}),
        tap((saved) => this.applyBranding({ ...this.brandingSubject.value, ...saved }))
      );
  }

  getBrandingSnapshot(): BrandingSettings {
    return this.brandingSubject.value;
  }

  getContrastColor(background: string | null | undefined): string {
    return this.getContrastText(
      this.normalizeHexColor(background) ?? DEFAULT_BRANDING_SETTINGS.buttonColor
    );
  }

  refreshFromServer(): Observable<BrandingSettings> {
    return this.api.get<BrandingSettings>('api/users/branding').pipe(
      map((settings) => settings ?? {}),
      tap((settings) => this.applyBranding(settings)),
      catchError(() => {
        const fallback = this.brandingSubject.value;
        this.applyBranding(fallback);
        return of(fallback);
      })
    );
  }

  private applyBranding(settings: BrandingSettings): void {
    const next = { ...settings };
    this.brandingSubject.next(next);

    if (typeof document === 'undefined') {
      return;
    }

    const root = document.documentElement;
    const primary =
      this.normalizeHexColor(settings.buttonColor) ?? DEFAULT_BRANDING_SETTINGS.buttonColor;
    const hover = this.shiftHexLightness(primary, -0.12);
    const contrast = this.getContrastColor(primary);
    const sidebar =
      this.normalizeHexColor(settings.sidebarBackground) ?? DEFAULT_BRANDING_SETTINGS.sidebarBackground;
    const sidebarContrast = this.getContrastColor(sidebar);

    root.style.setProperty(RUNTIME_TOKENS.primary, primary);
    root.style.setProperty(RUNTIME_TOKENS.onPrimary, contrast);
    root.style.setProperty(RUNTIME_TOKENS.primaryHover, hover);
    root.style.setProperty(RUNTIME_TOKENS.materialPrimary, primary);
    root.style.setProperty(RUNTIME_TOKENS.materialOnPrimary, contrast);
    root.style.setProperty(RUNTIME_TOKENS.sidebarOnBackground, sidebarContrast);
    root.style.setProperty(RUNTIME_TOKENS.legacyButtonColor, primary);

    this.setOptionalRuntimeToken(root, RUNTIME_TOKENS.publicBackground, settings.publicBackground);
    this.setOptionalRuntimeToken(root, RUNTIME_TOKENS.privateBackground, settings.privateBackground);
    this.setOptionalRuntimeToken(root, RUNTIME_TOKENS.sidebarBackground, settings.sidebarBackground);
  }

  private setOptionalRuntimeToken(root: HTMLElement, token: string, value: string | null | undefined): void {
    const normalized = value?.trim();
    if (normalized) {
      root.style.setProperty(token, normalized);
      return;
    }

    root.style.removeProperty(token);
  }

  private normalizeHexColor(value: string | null | undefined): string | null {
    if (!value) {
      return null;
    }

    const raw = value.trim();
    const shortHex = /^#([0-9a-fA-F]{3})$/;
    const longHex = /^#([0-9a-fA-F]{6})$/;

    if (longHex.test(raw)) {
      return raw.toLowerCase();
    }

    const shortMatch = raw.match(shortHex);
    if (shortMatch) {
      const [r, g, b] = shortMatch[1].split('');
      return `#${r}${r}${g}${g}${b}${b}`.toLowerCase();
    }

    return null;
  }

  private shiftHexLightness(hex: string, delta: number): string {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);

    const shift = (channel: number): number => {
      if (delta < 0) {
        return Math.round(channel * (1 + delta));
      }

      return Math.round(channel + (255 - channel) * delta);
    };

    const nr = Math.max(0, Math.min(255, shift(r)));
    const ng = Math.max(0, Math.min(255, shift(g)));
    const nb = Math.max(0, Math.min(255, shift(b)));

    return `#${nr.toString(16).padStart(2, '0')}${ng.toString(16).padStart(2, '0')}${nb.toString(16).padStart(2, '0')}`;
  }

  private getContrastText(hex: string): string {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    const luminance = this.relativeLuminance(r, g, b);
    const lightContrast = 1.05 / (luminance + 0.05);
    const darkLuminance = this.relativeLuminance(15, 23, 42);
    const darkContrast = (luminance + 0.05) / (darkLuminance + 0.05);

    return darkContrast >= lightContrast ? '#0f172a' : '#ffffff';
  }

  private relativeLuminance(r: number, g: number, b: number): number {
    const linearize = (channel: number): number => {
      const value = channel / 255;
      return value <= 0.04045 ? value / 12.92 : Math.pow((value + 0.055) / 1.055, 2.4);
    };

    return 0.2126 * linearize(r) + 0.7152 * linearize(g) + 0.0722 * linearize(b);
  }
}
