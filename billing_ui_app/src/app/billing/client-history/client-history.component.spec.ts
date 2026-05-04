import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClientHistoryComponent } from './client-history.component';

describe('ClientHistoryComponent', () => {
  let component: ClientHistoryComponent;
  let fixture: ComponentFixture<ClientHistoryComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ClientHistoryComponent]
    });
    fixture = TestBed.createComponent(ClientHistoryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
