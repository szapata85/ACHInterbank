import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { IntegrationMappingAdminService } from '../../../core/services/integration-mapping-admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { MappingEditorPageComponent } from './mapping-editor-page.component';

describe('MappingEditorPageComponent', () => {
  let component: MappingEditorPageComponent;
  let fixture: ComponentFixture<MappingEditorPageComponent>;

  const apiMock = {
    getMappingSetById: jasmine.createSpy().and.returnValue(
      of({
        id: 'set-1',
        methodId: 1,
        methodCode: 'WSCFAACH.Proc_Contrapartidas',
        name: 'Draft',
        version: 0,
        status: 'Draft',
        isActive: true,
        notes: '',
        publishedBy: '',
        rules: []
      })
    ),
    getMethodParameters: jasmine.createSpy().and.returnValue(of([{ id: 1, methodId: 1, parameterPath: 'CycleId', displayName: 'Cycle', dataType: 'string', cardinality: 'Scalar', required: true, sortOrder: 1, isActive: true }])),
    getSourceCatalog: jasmine.createSpy().and.returnValue(of([])),
    getTransformations: jasmine.createSpy().and.returnValue(of([])),
    upsertRules: jasmine.createSpy().and.returnValue(of({ id: 'set-1', methodId: 1, methodCode: 'WSCFAACH.Proc_Contrapartidas', name: 'Draft', version: 0, status: 'Draft', isActive: true, notes: '', publishedBy: '', rules: [] })),
    validate: jasmine.createSpy().and.returnValue(of({ mappingSetId: 'set-1', isValid: true, issues: [], coverage: { totalParameters: 1, validParameters: 1, incompleteParameters: 0, invalidParameters: 0, inactiveParameters: 0, coveredByDefaultOrFixed: 1, coveredBySourceField: 0 }, parameters: [{ parameterId: 1, parameterPath: 'CycleId', required: true, status: 'valid', resolutionKind: 'default-fixed', hints: ['ok'] }] })),
    preview: jasmine.createSpy().and.returnValue(of({ mappingSetId: 'set-1', methodId: 1, methodCode: 'WSCFAACH.Proc_Contrapartidas', contextMode: 'controlled-sample', items: [], payloadPreviewJson: '{}', rawPreviewJson: '[]' })),
    publish: jasmine.createSpy().and.returnValue(of({ id: 'set-1', methodId: 1, methodCode: 'WSCFAACH.Proc_Contrapartidas', name: 'Published', version: 1, status: 'Published', isActive: true, notes: '', publishedBy: 'ui-admin', rules: [] })),
    clone: jasmine.createSpy().and.returnValue(of({ id: 'set-2', methodId: 1, methodCode: 'WSCFAACH.Proc_Contrapartidas', name: 'Clone', version: 0, status: 'Draft', isActive: true, notes: '', publishedBy: '', rules: [] }))
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormsModule, ReactiveFormsModule, MappingEditorPageComponent],
      providers: [
        { provide: IntegrationMappingAdminService, useValue: apiMock },
        { provide: NotificationService, useValue: { success: jasmine.createSpy(), error: jasmine.createSpy() } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === 'mappingSetId' ? 'set-1' : 'WSCFAACH.Proc_Contrapartidas')
              }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MappingEditorPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render integration editor workspace', () => {
    expect(component).toBeTruthy();
    expect(apiMock.getMappingSetById).toHaveBeenCalled();
  });

  it('should run validation and preview workflows', () => {
    component.runValidation();
    component.runPreview();

    expect(apiMock.validate).toHaveBeenCalled();
    expect(apiMock.preview).toHaveBeenCalled();
  });

  it('should block publish when validation callback returns invalid', () => {
    spyOn(component, 'runValidation').and.callFake((done?: (isValid: boolean) => void) => done?.(false));

    component.publish();

    expect(apiMock.publish).not.toHaveBeenCalled();
  });
});
