import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { combineLatest, startWith } from 'rxjs';
import { AchErrorCode } from '../models/ach-error-code.model';
import { AchErrorCodesService } from '../services/ach-error-codes.service';

interface FilterState {
  search: string;
  category: string;
}

@Component({
  selector: 'app-ach-error-codes',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatTableModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  templateUrl: './ach-error-codes.component.html',
  styleUrls: ['./ach-error-codes.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchErrorCodesComponent implements OnInit {
  private readonly service = inject(AchErrorCodesService);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly categoryControl = new FormControl('All', { nonNullable: true });

  readonly displayedColumns = [
    'returnCode',
    'category',
    'standardDescription',
    'additionalDetail',
    'applicability'
  ];

  readonly dataSource = new MatTableDataSource<AchErrorCode>([]);
  categories: string[] = [];

  ngOnInit(): void {
    const codes = this.service.getAll();
    this.dataSource.data = codes;
    this.categories = ['All', ...Array.from(new Set(codes.map((code) => code.category)))];

    this.dataSource.filterPredicate = (data, rawFilter) => {
      const filter = JSON.parse(rawFilter) as FilterState;
      const search = filter.search.trim().toLowerCase();
      const category = filter.category;

      const matchesCategory = category === 'All' || data.category === category;
      if (!matchesCategory) {
        return false;
      }

      if (!search) {
        return true;
      }

      const haystack = [
        data.returnCode,
        data.category,
        data.standardDescription,
        data.additionalDetail,
        data.applicability
      ]
        .join(' ')
        .toLowerCase();

      return haystack.includes(search);
    };

    combineLatest([
      this.searchControl.valueChanges.pipe(startWith(this.searchControl.value)),
      this.categoryControl.valueChanges.pipe(startWith(this.categoryControl.value))
    ]).subscribe(([search, category]) => {
      this.dataSource.filter = JSON.stringify({ search, category });
    });
  }
}
