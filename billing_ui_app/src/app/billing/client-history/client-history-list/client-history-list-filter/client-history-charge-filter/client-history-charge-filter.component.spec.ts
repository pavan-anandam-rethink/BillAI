import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClientHistoryChargeFilterComponent } from './client-history-charge-filter.component';
import { describe, beforeEach, it } from 'node:test';

describe('ClientHistoryChargeFilterComponent', () => {
  let component: ClientHistoryChargeFilterComponent;
  let fixture: ComponentFixture<ClientHistoryChargeFilterComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ClientHistoryChargeFilterComponent]
    });
    fixture = TestBed.createComponent(ClientHistoryChargeFilterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
