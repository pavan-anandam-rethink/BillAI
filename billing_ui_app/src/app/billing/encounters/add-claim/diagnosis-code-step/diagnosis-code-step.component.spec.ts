import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DiagnosisCodeStepComponent } from './diagnosis-code-step.component';

describe('DiagnosisCodeStepComponent', () => {
  let component: DiagnosisCodeStepComponent;
  let fixture: ComponentFixture<DiagnosisCodeStepComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [DiagnosisCodeStepComponent]
    });
    fixture = TestBed.createComponent(DiagnosisCodeStepComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
