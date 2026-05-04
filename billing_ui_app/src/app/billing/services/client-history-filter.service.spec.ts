import { TestBed } from '@angular/core/testing';

import { ClientHistoryFilterService } from './client-history-filter.service';

describe('ClientHistoryFilterService', () => {
  let service: ClientHistoryFilterService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ClientHistoryFilterService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
