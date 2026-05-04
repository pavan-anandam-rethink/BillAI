import { TestBed } from '@angular/core/testing';

import { BackCancelService } from './back-cancel.service';

describe('BackCancelService', () => {
  let service: BackCancelService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BackCancelService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
