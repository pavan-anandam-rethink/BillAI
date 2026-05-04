import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BillingCodeNoteComponent } from './billing-code-note.component';

describe('BillingCodeNoteComponent', () => {
  let component: BillingCodeNoteComponent;
  let fixture: ComponentFixture<BillingCodeNoteComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [BillingCodeNoteComponent]
    });
    fixture = TestBed.createComponent(BillingCodeNoteComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
