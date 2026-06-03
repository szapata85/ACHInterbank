import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { NachaConfigProfileReadModel } from '../models/nacha-config-admin.models';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';

interface OfficialNachaRecordRow extends NachaConfigProfileReadModel {
  recordTypesDisplay: string;
  vigencia: string;
}

@Component({
  selector: 'app-nacha-config-records-page',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './nacha-config-records-page.component.html',
  styleUrls: ['./nacha-config-official-readonly-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigRecordsPageComponent implements OnInit {
  private readonly query = inject(NachaConfigQueryService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  profiles: OfficialNachaRecordRow[] = [];
  loading = false;
  loadError = '';

  readonly columns = [
    { key: 'profileCode', label: 'profileCode', width: '160px' },
    { key: 'clearingHouseCode', label: 'clearingHouseCode', width: '150px' },
    { key: 'flowType', label: 'flowType', width: '150px' },
    { key: 'status', label: 'status', width: '120px' },
    { key: 'version', label: 'version', width: '110px' },
    { key: 'recordTypesDisplay', label: 'recordTypes', width: '180px' },
    { key: 'layoutVariantCount', label: 'layoutVariantCount', width: '180px' },
    { key: 'fieldCount', label: 'fieldCount', width: '130px' },
    { key: 'vigencia', label: 'vigencia', width: '220px' }
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
            recordTypesDisplay: (profile.recordTypes ?? []).join(', ') || '-',
            vigencia: `${this.date(profile.effectiveFrom)} a ${profile.effectiveTo ? this.date(profile.effectiveTo) : 'abierta'}`
          }));
          this.cdr.markForCheck();
        },
        error: () => {
          this.profiles = [];
          this.loadError = 'No fue posible cargar records oficiales desde nacha-config profiles.';
          this.notifications.error('No fue posible cargar records oficiales NACHA Config.');
          this.cdr.markForCheck();
        }
      });
  }

  private date(value?: string | null): string {
    return value ? new Date(value).toLocaleDateString('es-CO') : '-';
  }
}
