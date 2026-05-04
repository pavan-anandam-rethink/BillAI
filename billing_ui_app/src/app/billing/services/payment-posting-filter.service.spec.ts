import { TestBed } from '@angular/core/testing';

import { PaymentPostingFilterService } from './payment-posting-filter.service';

describe('PaymentPostingFilterService', () => {
  let service: PaymentPostingFilterService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PaymentPostingFilterService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
