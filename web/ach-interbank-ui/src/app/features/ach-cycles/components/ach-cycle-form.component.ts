import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AchCyclesApiService, ClearingHousesApiService } from '../services/ach-cycles-api.service';
import { AchCycleSummary, ClearingHouseOption, SaveAchCycleRequest } from '../models/ach-cycle.model';
import { SharedModule } from '../../../shared/shared.module';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-ach-cycle-form',
  templateUrl: './ach-cycle-form.component.html',
  styleUrls: ['./ach-cycle-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [SharedModule, RouterModule]
})
export class AchCycleFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(AchCyclesApiService);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);

  clearingHouses: ClearingHouseOption[] = [];
  isEdit = false;
  cycleId: string | null = null;

  readonly form = this.fb.group({
    clearingHouseId: [null as number | null, Validators.required],
    date: ['', Validators.required],
    startTime: ['', Validators.required],
    endTime: ['', Validators.required],
    status: ['Activo', Validators.required]
  });

  ngOnInit(): void {
    this.clearingHouseApi.list().subscribe((items) => (this.clearingHouses = items));

    this.cycleId = this.route.snapshot.paramMap.get('id');
    if (this.cycleId) {
      this.isEdit = true;
      this.api.getById(this.cycleId).subscribe((cycle) => this.patch(cycle));
    }
  }

  private patch(cycle: AchCycleSummary): void {
    this.form.patchValue({
      clearingHouseId: cycle.clearingHouseId,
      date: cycle.date,
      startTime: cycle.startTime,
      endTime: cycle.endTime,
      status: cycle.status
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: SaveAchCycleRequest = this.form.value as SaveAchCycleRequest;
    const request$ = this.isEdit && this.cycleId
      ? this.api.update(this.cycleId, payload)
      : this.api.create(payload);

    request$.subscribe(() => this.router.navigate(['/ach-cycles']));
  }
}
