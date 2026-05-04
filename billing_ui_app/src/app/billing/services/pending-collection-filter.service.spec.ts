import { TestBed } from '@angular/core/testing';

import { PendingCollectionFilterService } from './pending-collection-filter.service';

describe('PendingCollectionFilterService', () => {
  let service: PendingCollectionFilterService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(PendingCollectionFilterService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
