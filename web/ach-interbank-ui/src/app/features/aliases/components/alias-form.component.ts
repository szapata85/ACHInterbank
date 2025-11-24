import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AliasesApiService } from '../services/aliases-api.service';
import { AliasSummary, SaveAliasRequest } from '../models/alias.model';

@Component({
  selector: 'app-alias-form',
  templateUrl: './alias-form.component.html',
  styleUrls: ['./alias-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AliasFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(AliasesApiService);

  isEdit = false;
  aliasId: string | null = null;

  readonly form = this.fb.group({
    alias: ['', Validators.required],
    accountNumber: ['', Validators.required],
    documentNumber: [''],
    phoneNumber: [''],
    ownerName: ['']
  });

  ngOnInit(): void {
    this.aliasId = this.route.snapshot.paramMap.get('id');
    if (this.aliasId) {
      this.isEdit = true;
      this.api.getById(this.aliasId).subscribe((alias) => this.patch(alias));
    }
  }

  private patch(alias: AliasSummary): void {
    this.form.patchValue({
      alias: alias.alias,
      accountNumber: alias.accountNumber,
      documentNumber: alias.documentNumber,
      phoneNumber: alias.phoneNumber,
      ownerName: alias.ownerName
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: SaveAliasRequest = this.form.value as SaveAliasRequest;
    const request$ = this.isEdit && this.aliasId
      ? this.api.update(this.aliasId, payload)
      : this.api.create(payload);

    request$.subscribe(() => this.router.navigate(['/aliases']));
  }
}
