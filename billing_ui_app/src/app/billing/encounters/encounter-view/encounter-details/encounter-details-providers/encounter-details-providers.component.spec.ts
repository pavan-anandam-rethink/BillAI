import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EncounterDetailsProvidersComponent } from './encounter-details-providers.component';

describe('EncounterDetailsProvidersComponent', () => {
  let component: EncounterDetailsProvidersComponent;
  let fixture: ComponentFixture<EncounterDetailsProvidersComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EncounterDetailsProvidersComponent]
    });
    fixture = TestBed.createComponent(EncounterDetailsProvidersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
