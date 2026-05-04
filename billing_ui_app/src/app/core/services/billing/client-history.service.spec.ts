import { TestBed } from '@angular/core/testing';

import { ClientHistoryService } from './client-history.service';

describe('ClientHistoryService', () => {
  let service: ClientHistoryService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ClientHistoryService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
