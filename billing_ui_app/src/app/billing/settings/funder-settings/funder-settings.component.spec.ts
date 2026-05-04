import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FunderSettingsComponent } from './funder-settings.component';

describe('FunderSettingsComponent', () => {
  let component: FunderSettingsComponent;
  let fixture: ComponentFixture<FunderSettingsComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [FunderSettingsComponent]
    });
    fixture = TestBed.createComponent(FunderSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
