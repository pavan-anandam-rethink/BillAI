import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from '@angular/core';
import { PaymentPostingService } from "@core/services/billing/payment-posting.service";
import { Observable, Subject, of } from 'rxjs';
import { PaymentPostingListSubject } from '@core/subjects/payment-posting-list.subject';
import { GridDataResult } from '@progress/kendo-angular-grid';
import { map } from "rxjs/operators";
import { ListFilterSort } from '@core/models/billing';
 
@Component({
  selector: 'filter-mode-popup',
  templateUrl: './payment-posting-list-filter-mode-popup.component.html',
  styleUrls: ['./payment-posting-list-filter-mode-popup.component.css']
})
export class PaymentPostingListFilterModePopupComponent {
  @Input() anchor: ElementRef;
  @Output() modeClickedEmmiter = new EventEmitter<any>();
  @Output() anchorViewportLeave = new EventEmitter();
  @Input() selectedModes: { modeName: string; checked: boolean }[] = [];
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;

  @HostListener('document:click', ['$event'])
  public documentClick(event: any): void {
    if (!this.contains(event.target)) {
      this.anchorViewportLeave.emit();
    }
  }

  private unsubscribe = new Subject();
  reconcileModes: any[] = [];
  totalCount: number = 0;
  isLoading: boolean = false;
  paymentPostingListSubject: PaymentPostingListSubject;
  view: Observable<GridDataResult>

  constructor(private paymentPostingService: PaymentPostingService) {
  }

  searchModes(): void {
    this.isLoading = true;
    const result = ['Manual','Electronic'];

    of(result).subscribe(data => {
    if (result) {

        result.forEach(ele => this.reconcileModes.push({modeName: ele, checked: false}));

        this.reconcileModes = this.reconcileModes.where((x: any) =>
          !this.selectedModes.any((p: any) => p.modeName === x.modeName));
        this.reconcileModes = this.selectedModes.concat(this.reconcileModes);

        this.totalCount = result.length;
        this.isLoading = false;
      }
      return result;
    });
  }


  modeClicked(mode) {
    this.modeClickedEmmiter.emit(mode);
  }

  private contains(target: any): boolean {
    return this.anchor.nativeElement.contains(target) ||
      (this.popup ? this.popup.nativeElement.contains(target) : false);
  }

ngOnDestroy() {
    this.unsubscribe.next(void 0);
    this.unsubscribe.complete();
  }

  ngOnInit() {
    this.searchModes();
  }
}
