import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClientHistoryChargeComponent } from './client-history-charge.component';

describe('ClientHistoryChargeComponent', () => {
  let component: ClientHistoryChargeComponent;
  let fixture: ComponentFixture<ClientHistoryChargeComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ClientHistoryChargeComponent]
    });
    fixture = TestBed.createComponent(ClientHistoryChargeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
