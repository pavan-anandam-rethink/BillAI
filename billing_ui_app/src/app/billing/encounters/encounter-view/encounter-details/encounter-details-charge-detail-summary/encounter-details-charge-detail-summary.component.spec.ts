import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EncounterDetailsChargeDetailSummaryComponent } from './encounter-details-charge-detail-summary.component';

describe('EncounterDetailsChargeDetailSummaryComponent', () => {
  let component: EncounterDetailsChargeDetailSummaryComponent;
  let fixture: ComponentFixture<EncounterDetailsChargeDetailSummaryComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EncounterDetailsChargeDetailSummaryComponent]
    });
    fixture = TestBed.createComponent(EncounterDetailsChargeDetailSummaryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
