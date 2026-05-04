import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FunderSettingsSaveComponent } from './funder-settings-save.component';

describe('FunderSettingsSaveComponent', () => {
  let component: FunderSettingsSaveComponent;
  let fixture: ComponentFixture<FunderSettingsSaveComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [FunderSettingsSaveComponent]
    });
    fixture = TestBed.createComponent(FunderSettingsSaveComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
