import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClaimFollowUpComponent } from './claim-follow-up.component';

describe('ClaimFollowUpComponent', () => {
  let component: ClaimFollowUpComponent;
  let fixture: ComponentFixture<ClaimFollowUpComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ClaimFollowUpComponent]
    });
    fixture = TestBed.createComponent(ClaimFollowUpComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
