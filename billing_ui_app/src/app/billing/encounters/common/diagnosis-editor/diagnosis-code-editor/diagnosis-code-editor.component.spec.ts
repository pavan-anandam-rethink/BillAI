import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DiagnosisCodeEditorComponent } from './diagnosis-code-editor.component';

describe('DiagnosisCodeEditorComponent', () => {
  let component: DiagnosisCodeEditorComponent;
  let fixture: ComponentFixture<DiagnosisCodeEditorComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [DiagnosisCodeEditorComponent]
    });
    fixture = TestBed.createComponent(DiagnosisCodeEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
