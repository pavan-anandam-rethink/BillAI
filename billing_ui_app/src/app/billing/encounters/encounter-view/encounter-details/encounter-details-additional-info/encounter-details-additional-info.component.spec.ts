import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EncounterDetailsAdditionalInfoComponent } from './encounter-details-additional-info.component';

describe('EncounterDetailsAdditionalInfoComponent', () => {
  let component: EncounterDetailsAdditionalInfoComponent;
  let fixture: ComponentFixture<EncounterDetailsAdditionalInfoComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EncounterDetailsAdditionalInfoComponent]
    });
    fixture = TestBed.createComponent(EncounterDetailsAdditionalInfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
