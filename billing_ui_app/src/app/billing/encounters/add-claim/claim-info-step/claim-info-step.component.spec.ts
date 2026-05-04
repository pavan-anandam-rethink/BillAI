import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClaimInfoStepComponent } from './claim-info-step.component';

describe('ClaimInfoStepComponent', () => {
  let component: ClaimInfoStepComponent;
  let fixture: ComponentFixture<ClaimInfoStepComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ClaimInfoStepComponent]
    });
    fixture = TestBed.createComponent(ClaimInfoStepComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
