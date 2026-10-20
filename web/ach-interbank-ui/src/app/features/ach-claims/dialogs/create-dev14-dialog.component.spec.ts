import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { MatDialogRef } from '@angular/material/dialog';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { AchClaimsApiService } from '../ach-claims-api.service';
import { CreateDev14DialogComponent } from './create-dev14-dialog.component';

describe('CreateDev14DialogComponent', () => {
  let fixture: ComponentFixture<CreateDev14DialogComponent>;
  let component: CreateDev14DialogComponent;
  let api: jasmine.SpyObj<AchClaimsApiService>;
  let dialogRef: jasmine.SpyObj<MatDialogRef<CreateDev14DialogComponent>>;

  beforeEach(async () => {
    api = jasmine.createSpyObj('AchClaimsApiService', ['createDev14']);
    api.createDev14.and.returnValue(of({ id: 30 } as never));
    dialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);

    await TestBed.configureTestingModule({
      imports: [CreateDev14DialogComponent],
      providers: [
        provideNoopAnimations(),
        { provide: AchClaimsApiService, useValue: api },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success']) }
      ]
    }).compileComponents();
    fixture = TestBed.createComponent(CreateDev14DialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('exige al menos una transacción', () => {
    component.submit();
    expect(api.createDev14).not.toHaveBeenCalled();
    expect(component.form.controls.transactionIdsText.hasError('required')).toBeTrue();
  });

  it('elimina transacciones duplicadas antes de crear', () => {
    component.form.patchValue({ transactionIdsText: '10, 20 10;20', externalReference: ' REF-1 ' });
    component.submit();
    expect(api.createDev14).toHaveBeenCalledWith({ transactionIds: [10, 20], externalReference: 'REF-1' }, jasmine.any(String));
    expect(dialogRef.close).toHaveBeenCalledWith(jasmine.objectContaining({ id: 30 }));
  });

  it('presenta un error funcional de forma comprensible', () => {
    api.createDev14.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 400,
      error: { detail: 'Una de las transacciones no corresponde a un débito ACH Colombia.' }
    })));
    component.form.patchValue({ transactionIdsText: '10' });

    component.submit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Una de las transacciones no corresponde a un débito ACH Colombia.');
    expect(fixture.nativeElement.textContent).not.toContain('BadRequest');
  });

  it('rechaza valores que no sean números enteros positivos', () => {
    component.form.patchValue({ transactionIdsText: '10, abc, -2' });
    component.submit();
    expect(api.createDev14).not.toHaveBeenCalled();
    expect(component.form.controls.transactionIdsText.hasError('invalidIds')).toBeTrue();
  });
});
