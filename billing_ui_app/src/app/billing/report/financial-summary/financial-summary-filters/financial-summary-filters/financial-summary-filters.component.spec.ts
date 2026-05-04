import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FinancialSummaryFiltersComponent } from './financial-summary-filters.component';

describe('FinancialSummaryFiltersComponent', () => {
  let component: FinancialSummaryFiltersComponent;
  let fixture: ComponentFixture<FinancialSummaryFiltersComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [FinancialSummaryFiltersComponent]
    });
    fixture = TestBed.createComponent(FinancialSummaryFiltersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
