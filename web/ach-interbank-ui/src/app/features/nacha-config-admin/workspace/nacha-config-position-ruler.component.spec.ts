import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NachaConfigPositionRulerComponent } from './nacha-config-position-ruler.component';

describe('NachaConfigPositionRulerComponent UX3', () => {
  let fixture: ComponentFixture<NachaConfigPositionRulerComponent>;
  let component: NachaConfigPositionRulerComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [NachaConfigPositionRulerComponent] }).compileComponents();
    fixture = TestBed.createComponent(NachaConfigPositionRulerComponent);
    component = fixture.componentInstance;
    component.variant = {
      id: 1, recordCode: '6', variantCode: 'R6', nombreEs: 'Detalle', priority: 1,
      isDefaultForRecord: true, totalLength: 106,
      fields: [
        { id: 1, fieldCode: 'A', fieldNameEs: 'Campo A', startPosition: 1, length: 10, sourceDefinitionId: 1, isEnabled: true, reglas: [] },
        { id: 2, fieldCode: 'B', fieldNameEs: 'Campo B', startPosition: 8, length: 6, sourceDefinitionId: 2, isEnabled: false, reglas: [] }
      ]
    };
    component.selectedFieldId = 1;
    fixture.detectChanges();
  });

  it('renders the configured total length and marks overlap/inactive state from the read model', () => {
    expect(fixture.nativeElement.textContent).toContain('106 posiciones');
    expect(component.hasOverlap(component.fields[0])).toBeTrue();
    expect(fixture.nativeElement.querySelectorAll('.overlap').length).toBe(2);
    expect(fixture.nativeElement.querySelectorAll('.disabled-field').length).toBe(1);
  });

  it('selects a field with a keyboard-accessible button and descriptive aria label', () => {
    spyOn(component.selectedFieldIdChange, 'emit');
    const buttons = fixture.nativeElement.querySelectorAll('button.field-block') as NodeListOf<HTMLButtonElement>;
    expect(buttons[0].getAttribute('aria-label')).toContain('posiciones 1 a 10');
    buttons[1].click();
    expect(component.selectedFieldIdChange.emit).toHaveBeenCalledWith(2);
  });
});
