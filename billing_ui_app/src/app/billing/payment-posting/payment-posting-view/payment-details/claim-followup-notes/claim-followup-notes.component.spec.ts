import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClaimFollowupNotesComponent } from './claim-followup-notes.component';

describe('ClaimFollowupNotesComponent', () => {
  let component: ClaimFollowupNotesComponent;
  let fixture: ComponentFixture<ClaimFollowupNotesComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ClaimFollowupNotesComponent]
    });
    fixture = TestBed.createComponent(ClaimFollowupNotesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
