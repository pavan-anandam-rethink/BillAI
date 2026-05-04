import { TestBed } from '@angular/core/testing';

import { ReportingFilterService } from './reporting-filter.service';

describe('ReportingFilterService', () => {
  let service: ReportingFilterService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ReportingFilterService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
