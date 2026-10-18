import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import {
  OperationalConfirmDialogComponent,
  OperationalConfirmDialogData
} from './operational-confirm-dialog.component';

describe('OperationalConfirmDialogComponent', () => {
  let fixture: ComponentFixture<OperationalConfirmDialogComponent>;

  beforeEach(async () => {
    const data: OperationalConfirmDialogData = {
      title: 'Eliminar configuración',
      message: 'Esta acción no se puede deshacer.',
      confirmLabel: 'Eliminar',
      cancelLabel: 'Conservar',
      destructive: true
    };

    await TestBed.configureTestingModule({
      imports: [OperationalConfirmDialogComponent],
      providers: [{ provide: MAT_DIALOG_DATA, useValue: data }]
    }).compileComponents();

    fixture = TestBed.createComponent(OperationalConfirmDialogComponent);
    fixture.detectChanges();
  });

  it('renders title, message, confirmation controls and destructive state', () => {
    const host = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(host.querySelectorAll('button'));

    expect(host.querySelector('[mat-dialog-title]')?.textContent).toContain('Eliminar configuración');
    expect(host.querySelector('mat-dialog-content')?.textContent).toContain(
      'Esta acción no se puede deshacer.'
    );
    expect(buttons.map((button) => button.textContent?.trim())).toEqual(['Conservar', 'Eliminar']);
    expect(buttons[1].classList.contains('destructive-action')).toBeTrue();
  });
});
