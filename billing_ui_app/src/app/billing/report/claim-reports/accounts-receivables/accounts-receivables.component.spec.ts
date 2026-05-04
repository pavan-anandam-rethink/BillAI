import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccountsReceivablesComponent } from './accounts-receivables.component';

describe('AccountsReceivablesComponent', () => {
  let component: AccountsReceivablesComponent;
  let fixture: ComponentFixture<AccountsReceivablesComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AccountsReceivablesComponent]
    });
    fixture = TestBed.createComponent(AccountsReceivablesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
