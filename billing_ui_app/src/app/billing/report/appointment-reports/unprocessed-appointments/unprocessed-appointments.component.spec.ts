import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UnprocessedAppointmentsComponent } from './unprocessed-appointments.component';

describe('UnprocessedAppointmentsComponent', () => {
  let component: UnprocessedAppointmentsComponent;
  let fixture: ComponentFixture<UnprocessedAppointmentsComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [UnprocessedAppointmentsComponent]
    });
    fixture = TestBed.createComponent(UnprocessedAppointmentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
