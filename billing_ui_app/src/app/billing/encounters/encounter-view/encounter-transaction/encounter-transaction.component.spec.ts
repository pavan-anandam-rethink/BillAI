import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EncounterTransactionComponent } from './encounter-transaction.component';

describe('EncounterTransactionComponent', () => {
  let component: EncounterTransactionComponent;
  let fixture: ComponentFixture<EncounterTransactionComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EncounterTransactionComponent]
    });
    fixture = TestBed.createComponent(EncounterTransactionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
