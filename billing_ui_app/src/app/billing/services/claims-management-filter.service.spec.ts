import { TestBed } from '@angular/core/testing';

import { ClaimsManagementFilterService } from './claims-management-filter.service';

describe('ClaimsManagementFilterService', () => {
  let service: ClaimsManagementFilterService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ClaimsManagementFilterService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
