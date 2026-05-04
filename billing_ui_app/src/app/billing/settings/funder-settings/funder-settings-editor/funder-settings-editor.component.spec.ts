import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FunderSettingsEditorComponent } from './funder-settings-editor.component';

describe('FunderSettingsEditorComponent', () => {
  let component: FunderSettingsEditorComponent;
  let fixture: ComponentFixture<FunderSettingsEditorComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [FunderSettingsEditorComponent]
    });
    fixture = TestBed.createComponent(FunderSettingsEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
