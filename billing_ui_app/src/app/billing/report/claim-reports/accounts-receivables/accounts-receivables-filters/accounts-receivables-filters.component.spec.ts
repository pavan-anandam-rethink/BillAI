import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccountsReceivablesFiltersComponent } from './accounts-receivables-filters.component';

describe('AccountsReceivablesFiltersComponent', () => {
  let component: AccountsReceivablesFiltersComponent;
  let fixture: ComponentFixture<AccountsReceivablesFiltersComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AccountsReceivablesFiltersComponent]
    });
    fixture = TestBed.createComponent(AccountsReceivablesFiltersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
