import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClientHistoryListFilterComponent } from './client-history-list-filter.component';

describe('ClientHistoryListFilterComponent', () => {
  let component: ClientHistoryListFilterComponent;
  let fixture: ComponentFixture<ClientHistoryListFilterComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ClientHistoryListFilterComponent]
    });
    fixture = TestBed.createComponent(ClientHistoryListFilterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
