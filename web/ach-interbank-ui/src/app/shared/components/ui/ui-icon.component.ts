import { ChangeDetectionStrategy, Component, HostBinding, Input } from '@angular/core';

const FALLBACK_ICON = 'help';

const SUPPORTED_MATERIAL_SYMBOLS = new Set([
  'account_balance',
  'add',
  'analytics',
  'assignment',
  'assignment_return',
  'block',
  'category',
  'check_circle',
  'credit_card',
  'dashboard',
  'download',
  'edit',
  'event',
  'event_busy',
  'fact_check',
  'file_download',
  'folder',
  'group',
  'groups',
  'help',
  'history',
  'home',
  'hub',
  'inbox',
  'inventory',
  'list',
  'list_alt',
  'lock',
  'lock_clock',
  'login',
  'manage_accounts',
  'manage_search',
  'menu',
  'note_add',
  'palette',
  'payments',
  'playlist_add_check',
  'policy',
  'receipt_long',
  'refresh',
  'rule',
  'save',
  'schedule',
  'schema',
  'science',
  'search',
  'security',
  'settings',
  'settings_ethernet',
  'summarize',
  'support',
  'swap_horiz',
  'sync',
  'timer',
  'tune',
  'upload',
  'upload_file',
  'view_column',
  'view_list',
  'visibility',
  'visibility_off'
]);

@Component({
  selector: 'app-ui-icon',
  standalone: true,
  template: `
    <span class="material-symbols-outlined glyph" aria-hidden="true">{{ resolvedIcon }}</span>
  `,
  styles: [`
    :host {
      display: inline-grid;
      place-items: center;
      width: 1.5rem;
      height: 1.5rem;
      flex: 0 0 1.5rem;
      line-height: 1;
    }

    .glyph {
      display: inline-block;
      max-width: 1em;
      overflow: hidden;
      font-size: inherit;
      line-height: 1;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiIconComponent {
  private iconKey = '';

  resolvedIcon = FALLBACK_ICON;

  @Input()
  set name(value: string | null | undefined) {
    this.iconKey = (value ?? '').trim().toLowerCase();
    this.resolvedIcon = SUPPORTED_MATERIAL_SYMBOLS.has(this.iconKey)
      ? this.iconKey
      : FALLBACK_ICON;
  }

  @HostBinding('attr.data-icon-key')
  get dataIconKey(): string {
    return this.iconKey;
  }

  @HostBinding('attr.data-icon-resolved')
  get dataIconResolved(): string {
    return this.resolvedIcon;
  }
}
