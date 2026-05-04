import { TestBed } from '@angular/core/testing';

import { CreateInvoiceFilterService } from './create-invoice-filter.service';

describe('PatientInvoiceFilterService', () => {
  let service: CreateInvoiceFilterService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CreateInvoiceFilterService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
