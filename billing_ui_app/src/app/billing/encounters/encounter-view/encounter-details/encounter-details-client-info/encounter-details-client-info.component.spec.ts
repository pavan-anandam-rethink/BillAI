import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EncounterDetailsClientInfoComponent } from './encounter-details-client-info.component';

describe('EncounterDetailsClientInfoComponent', () => {
  let component: EncounterDetailsClientInfoComponent;
  let fixture: ComponentFixture<EncounterDetailsClientInfoComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EncounterDetailsClientInfoComponent]
    });
    fixture = TestBed.createComponent(EncounterDetailsClientInfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
