import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChargeNotesComponent } from './charge-notes.component';

describe('ChargeNotesComponent', () => {
  let component: ChargeNotesComponent;
  let fixture: ComponentFixture<ChargeNotesComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ChargeNotesComponent]
    });
    fixture = TestBed.createComponent(ChargeNotesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
