import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BulkPostingComponent } from './bulk-posting.component';

describe('BulkPostingComponent', () => {
  let component: BulkPostingComponent;
  let fixture: ComponentFixture<BulkPostingComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [BulkPostingComponent]
    });
    fixture = TestBed.createComponent(BulkPostingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
