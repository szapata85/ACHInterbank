import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { take } from 'rxjs';
import { NachaConfigQueryService } from '../services/nacha-config-query.service';

@Component({
  selector: 'app-nacha-config-legacy-workspace-redirect',
  standalone: true,
  template: '<p role="status" aria-live="polite">Abriendo la parametrización NACHA-M…</p>',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaConfigLegacyWorkspaceRedirectComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly query = inject(NachaConfigQueryService);

  ngOnInit(): void {
    const requestedId = Number(this.route.snapshot.queryParamMap.get('profileId'));
    if (Number.isFinite(requestedId) && requestedId > 0) {
      this.openWorkspace(requestedId);
      return;
    }

    this.query.perfiles().pipe(take(1)).subscribe({
      next: (profiles) => profiles.length ? this.openWorkspace(profiles[0].id) : this.openProfiles(),
      error: () => this.openProfiles()
    });
  }

  private openWorkspace(profileId: number): void {
    const queryParams = { ...this.route.snapshot.queryParams } as Record<string, unknown>;
    delete queryParams['profileId'];
    void this.router.navigate(['/nacha-config-admin/perfiles', profileId], { queryParams, replaceUrl: true });
  }

  private openProfiles(): void {
    void this.router.navigate(['/nacha-config-admin/perfiles'], { replaceUrl: true });
  }
}
