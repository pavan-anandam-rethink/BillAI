import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EncounterViewComponent } from './encounter-view.component';

describe('EncounterViewComponent', () => {
  let component: EncounterViewComponent;
  let fixture: ComponentFixture<EncounterViewComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [EncounterViewComponent]
    });
    fixture = TestBed.createComponent(EncounterViewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
