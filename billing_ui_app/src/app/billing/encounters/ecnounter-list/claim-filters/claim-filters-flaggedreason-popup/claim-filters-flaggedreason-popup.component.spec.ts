import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClaimFiltersFlaggedreasonPopupComponent } from './claim-filters-flaggedreason-popup.component';

describe('ClaimFiltersFlaggedreasonPopupComponent', () => {
  let component: ClaimFiltersFlaggedreasonPopupComponent;
  let fixture: ComponentFixture<ClaimFiltersFlaggedreasonPopupComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ClaimFiltersFlaggedreasonPopupComponent]
    });
    fixture = TestBed.createComponent(ClaimFiltersFlaggedreasonPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
