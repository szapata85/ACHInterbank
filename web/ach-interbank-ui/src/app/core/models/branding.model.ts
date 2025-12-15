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
