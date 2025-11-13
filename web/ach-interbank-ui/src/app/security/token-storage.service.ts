import { Inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

const ACCESS_TOKEN_KEY = 'ach.interbank.access_token';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private readonly storage: Storage | null;

  constructor(@Inject(PLATFORM_ID) platformId: Object) {
    this.storage = isPlatformBrowser(platformId) ? window.sessionStorage : null;
  }

  setAccessToken(token: string): void {
    this.storage?.setItem(ACCESS_TOKEN_KEY, token);
  }

  getAccessToken(): string | null {
    return this.storage?.getItem(ACCESS_TOKEN_KEY) ?? null;
  }

  clear(): void {
    this.storage?.removeItem(ACCESS_TOKEN_KEY);
  }
}
