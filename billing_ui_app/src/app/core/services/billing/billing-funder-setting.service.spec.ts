import { TestBed } from '@angular/core/testing';

import { BillingFunderSettingService } from './billing-funder-setting.service';

describe('BillingFunderSettingService', () => {
  let service: BillingFunderSettingService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BillingFunderSettingService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
