import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClaimFollowUpFiltersComponent } from './claim-follow-up-filters.component';

describe('ClaimFollowUpFiltersComponent', () => {
  let component: ClaimFollowUpFiltersComponent;
  let fixture: ComponentFixture<ClaimFollowUpFiltersComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ClaimFollowUpFiltersComponent]
    });
    fixture = TestBed.createComponent(ClaimFollowUpFiltersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
