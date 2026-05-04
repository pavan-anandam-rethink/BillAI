import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FunderPopupComponent } from './funder-popup.component';

describe('FunderPopupComponent', () => {
  let component: FunderPopupComponent;
  let fixture: ComponentFixture<FunderPopupComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [FunderPopupComponent]
    });
    fixture = TestBed.createComponent(FunderPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
