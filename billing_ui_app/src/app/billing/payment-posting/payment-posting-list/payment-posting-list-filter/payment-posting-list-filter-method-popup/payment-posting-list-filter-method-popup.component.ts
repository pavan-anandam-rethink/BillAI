import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from '@angular/core';
import { PaymentPostingMethods } from '@core/models/billing';
import { PaymentPostingService } from '@core/services/billing';
import { Subject } from 'rxjs';

@Component({
  selector: 'filter-method-popup',
  templateUrl: './payment-posting-list-filter-method-popup.component.html',
  styleUrls: ['./payment-posting-list-filter-method-popup.component.css']
})
export class PaymentPostingListFilterMethodPopupComponent {
@Input() anchor: ElementRef;
  @Output() methodClickedEmmiter = new EventEmitter<any>();
  @Output() anchorViewportLeave = new EventEmitter();
  @Input() selectedPaymentMethods: PaymentPostingMethods[] = [];
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;

  @HostListener('document:click', ['$event'])
  public documentClick(event: any): void {
    if (!this.contains(event.target)) {
      this.anchorViewportLeave.emit();
    }
  }

  private unsubscribe = new Subject();

  paymentMethods: any[] = [];
  totalCount: number = 0;

  isLoading: boolean = false;
  constructor(private paymentPostingService: PaymentPostingService) {
   this.searchStatuses();
  }

  searchStatuses(): void {
    this.isLoading = true;
    this.paymentPostingService.getPaymentMethods().subscribe(result => {
      if (result) {

        result.forEach(ele => this.paymentMethods.push({displayName: ele.displayName, enumValue: ele.enumValue, checked: false}));

        this.paymentMethods = this.paymentMethods.where((x: any) =>
          !this.selectedPaymentMethods.any((p: any) => p.displayName === x.displayName));
        this.paymentMethods = this.selectedPaymentMethods.concat(this.paymentMethods);

        this.totalCount = result.length;
        this.isLoading = false;
      }
      return result;
    });
  }

  methodClicked(method) {
    this.methodClickedEmmiter.emit(method);
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
  }
}
