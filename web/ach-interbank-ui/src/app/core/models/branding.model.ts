export interface BrandingSettings {
  /** Logo utilizado en la parte pública (login). */
  publicLogo?: string | null;
  /** Logo utilizado en la parte privada (dashboard/menu). */
  privateLogo?: string | null;
  /** Color o gradiente para el fondo público (pantalla de login). */
  publicBackground?: string | null;
  /** Color o gradiente para el fondo privado (panel principal). */
  privateBackground?: string | null;
  /** Color para el fondo del menú lateral en el portal privado. */
  sidebarBackground?: string | null;
  /** Color principal de los botones a nivel de aplicación. */
  buttonColor?: string | null;
}

// BrandingSettings intentionally has no product-name field in 4C.1. Keep the
// product fallback centralized without expanding the backend contract.
export const DEFAULT_PRODUCT_NAME = 'ACH Interbank';

export const DEFAULT_BRANDING_SETTINGS = {
  publicBackground: '#f4f7fb',
  privateBackground: '#f8fafc',
  sidebarBackground: '#0f172a',
  buttonColor: '#2563eb'
} as const;
