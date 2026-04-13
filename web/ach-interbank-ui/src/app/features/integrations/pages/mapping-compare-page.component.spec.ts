import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { IntegrationMappingAdminService } from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { MappingComparePageComponent } from './mapping-compare-page.component';

describe('MappingComparePageComponent', () => {
  let component: MappingComparePageComponent;
  let fixture: ComponentFixture<MappingComparePageComponent>;

  const apiMock = {
    getMethods: jasmine.createSpy().and.returnValue(of([{ id: 1, code: 'WSCFAACH.Proc_Contrapartidas', displayName: 'Proc', soapClientCode: 'WSC', isActive: true }])),
    getMappingSets: jasmine.createSpy().and.returnValue(of([
      { id: 'a', methodId: 1, methodCode: 'WSCFAACH.Proc_Contrapartidas', name: 'V1', version: 1, status: 'Published', isActive: true, notes: '', publishedBy: 'u1', rules: [] },
      { id: 'b', methodId: 1, methodCode: 'WSCFAACH.Proc_Contrapartidas', name: 'V2', version: 2, status: 'Draft', isActive: true, notes: '', publishedBy: '', rules: [] }
    ])),
    compare: jasmine.createSpy().and.returnValue(of({
      left: { mappingSetId: 'a', name: 'V1', version: 1, status: 'Published', publishedBy: 'u1', notes: '' },
      right: { mappingSetId: 'b', name: 'V2', version: 2, status: 'Draft', publishedBy: '', notes: '' },
      rules: [{ parameterId: 1, parameterPath: 'CycleId', parameterGroup: 'ciclo-camara', changeType: 'Modified', changedFields: ['Priority'], potentialImpact: 'Impacto', left: null, right: null }]
    }))
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormsModule, MappingComparePageComponent],
      providers: [
        { provide: IntegrationMappingAdminService, useValue: apiMock },
        { provide: NotificationService, useValue: { error: jasmine.createSpy(), success: jasmine.createSpy() } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'WSCFAACH.Proc_Contrapartidas' } } } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MappingComparePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads versions and performs comparison', () => {
    expect(apiMock.getMappingSets).toHaveBeenCalled();
    component.runCompare();
    expect(apiMock.compare).toHaveBeenCalled();
  });
});
