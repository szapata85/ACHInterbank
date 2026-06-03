import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { NachaConfigProfileReadModel } from '../models/nacha-config-admin.models';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';

interface OfficialNachaConfigRow extends NachaConfigProfileReadModel {
  direction: string;
  serviceClassCode: string;
}

@Component({
  selector: 'app-nacha-config-variants-fields-page',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './nacha-config-variants-fields-page.component.html',
  styleUrls: ['./nacha-config-official-readonly-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigVariantsFieldsPageComponent implements OnInit {
  private readonly query = inject(NachaConfigQueryService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  profiles: OfficialNachaConfigRow[] = [];
  loading = false;
  loadError = '';

  readonly columns = [
    { key: 'profileCode', label: 'profileCode', width: '160px' },
    { key: 'profileName', label: 'profileName', width: '240px' },
    { key: 'clearingHouseCode', label: 'clearingHouseCode', width: '150px' },
    { key: 'flowType', label: 'flowType', width: '150px' },
    { key: 'direction', label: 'direction', width: '130px' },
    { key: 'serviceClassCode', label: 'serviceClassCode', width: '170px' },
    { key: 'status', label: 'status', width: '120px' },
    { key: 'version', label: 'version', width: '110px' },
    { key: 'effectiveFrom', label: 'effectiveFrom', width: '150px' },
    { key: 'effectiveTo', label: 'effectiveTo', width: '150px' },
    { key: 'layoutVariantCount', label: 'layoutVariantCount', width: '180px' },
    { key: 'fieldCount', label: 'fieldCount', width: '130px' }
  ];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.loadError = '';

    this.query.perfilesReadOnly()
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (profiles) => {
          this.profiles = profiles.map((profile) => ({
            ...profile,
            direction: this.resolveDirection(profile),
            serviceClassCode: this.resolveServiceClassCode(profile)
          }));
          this.cdr.markForCheck();
        },
        error: () => {
          this.profiles = [];
          this.loadError = 'No fue posible cargar nacha-config profiles oficiales.';
          this.notifications.error('No fue posible cargar nacha-config profiles oficiales.');
          this.cdr.markForCheck();
        }
      });
  }

  private resolveDirection(profile: NachaConfigProfileReadModel): string {
    const text = `${profile.profileCode} ${profile.profileName} ${profile.flowType}`.toLowerCase();
    if (text.includes('incoming') || text.includes('entrada')) return 'Incoming';
    if (text.includes('outgoing') || text.includes('salida')) return 'Outgoing';
    if (text.includes('return') || text.includes('devolucion')) return 'Return';
    return '-';
  }

  private resolveServiceClassCode(profile: NachaConfigProfileReadModel): string {
    const match = `${profile.profileCode} ${profile.profileName}`.match(/\b(200|220|225)\b/);
    return match?.[1] ?? '-';
  }
}
