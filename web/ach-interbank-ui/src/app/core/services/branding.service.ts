import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { BrandingSettings } from '../models/branding.model';

const STORAGE_KEY = 'ach-branding-settings';

@Injectable({ providedIn: 'root' })
export class BrandingService {
  private readonly brandingSubject = new BehaviorSubject<BrandingSettings>(this.loadFromStorage());
  readonly branding$: Observable<BrandingSettings> = this.brandingSubject.asObservable();

  updateBranding(settings: Partial<BrandingSettings>): void {
    const next: BrandingSettings = { ...this.brandingSubject.value, ...settings };
    this.brandingSubject.next(next);
    this.saveToStorage(next);
  }

  getBrandingSnapshot(): BrandingSettings {
    return this.brandingSubject.value;
  }

  private loadFromStorage(): BrandingSettings {
    if (typeof localStorage === 'undefined') {
      return {};
    }

    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) {
      return {};
    }

    try {
      return JSON.parse(stored) as BrandingSettings;
    } catch {
      return {};
    }
  }

  private saveToStorage(settings: BrandingSettings): void {
    if (typeof localStorage === 'undefined') {
      return;
    }

    localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
  }
}
