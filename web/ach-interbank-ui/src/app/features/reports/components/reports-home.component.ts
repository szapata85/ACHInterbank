import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { AccountingReviewExportComponent } from './accounting-review-export.component';

@Component({
  selector: 'app-reports-home',
  standalone: true,
  imports: [SharedModule, RouterModule, AccountingReviewExportComponent],
  templateUrl: './reports-home.component.html',
  styleUrls: ['./reports-home.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsHomeComponent {}
