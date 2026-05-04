import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProviderStepComponent } from './provider-step.component';

describe('ProviderStepComponent', () => {
  let component: ProviderStepComponent;
  let fixture: ComponentFixture<ProviderStepComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ProviderStepComponent]
    });
    fixture = TestBed.createComponent(ProviderStepComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
