import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClientHistoryInvoiceComponent } from './client-history-invoice.component';

describe('ClientHistoryInvoiceComponent', () => {
  let component: ClientHistoryInvoiceComponent;
  let fixture: ComponentFixture<ClientHistoryInvoiceComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ClientHistoryInvoiceComponent]
    });
    fixture = TestBed.createComponent(ClientHistoryInvoiceComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
