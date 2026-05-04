import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UnprocessedAppointmentsFiltersComponent } from './unprocessed-appointments-filters.component';

describe('UnprocessedAppointmentsFiltersComponent', () => {
  let component: UnprocessedAppointmentsFiltersComponent;
  let fixture: ComponentFixture<UnprocessedAppointmentsFiltersComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [UnprocessedAppointmentsFiltersComponent]
    });
    fixture = TestBed.createComponent(UnprocessedAppointmentsFiltersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
