import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ClientPatientInvoiceFilterComponent } from './client-history-invoice-filter.component';
import { beforeEach, describe, it } from 'node:test';

describe('ClientPatientInvoiceFilterComponent', () => {
  let component: ClientPatientInvoiceFilterComponent;
  let fixture: ComponentFixture<ClientPatientInvoiceFilterComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ClientPatientInvoiceFilterComponent]
    });
    fixture = TestBed.createComponent(ClientPatientInvoiceFilterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
function expect(component: ClientPatientInvoiceFilterComponent) {
  throw new Error('Function not implemented.');
}

