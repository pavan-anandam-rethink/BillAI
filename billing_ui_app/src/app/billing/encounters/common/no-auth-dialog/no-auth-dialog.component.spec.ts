import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NoAuthDialogComponent } from './no-auth-dialog.component';

describe('NoAuthDialogComponent', () => {
  let component: NoAuthDialogComponent;
  let fixture: ComponentFixture<NoAuthDialogComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [NoAuthDialogComponent]
    });
    fixture = TestBed.createComponent(NoAuthDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
