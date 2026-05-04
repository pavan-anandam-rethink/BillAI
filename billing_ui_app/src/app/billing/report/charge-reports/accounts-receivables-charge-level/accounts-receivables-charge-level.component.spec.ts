import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccountsReceivablesChargeLevelComponent } from './accounts-receivables-charge-level.component';

describe('AccountsReceivablesChargeLevelComponent', () => {
  let component: AccountsReceivablesChargeLevelComponent;
  let fixture: ComponentFixture<AccountsReceivablesChargeLevelComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [AccountsReceivablesChargeLevelComponent]
    });
    fixture = TestBed.createComponent(AccountsReceivablesChargeLevelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
