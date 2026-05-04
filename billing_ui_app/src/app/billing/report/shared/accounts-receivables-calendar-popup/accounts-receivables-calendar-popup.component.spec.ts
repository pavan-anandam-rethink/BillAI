import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccountsReceivablesCalendarPopupComponent } from './accounts-receivables-calendar-popup.component';

describe('AccountsReceivablesCalendarPopupComponent', () => {
  let component: AccountsReceivablesCalendarPopupComponent;
  let fixture: ComponentFixture<AccountsReceivablesCalendarPopupComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AccountsReceivablesCalendarPopupComponent]
    });
    fixture = TestBed.createComponent(AccountsReceivablesCalendarPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
