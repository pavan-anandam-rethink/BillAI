import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from '@angular/core';
import { PaymentPostingListFunderSearch, PaymentPostingListFunderSearchBase } from '@core/models/billing';
import { PaymentPostingService } from '@core/services/billing';
import { Observable, Subject } from 'rxjs';

@Component({
  selector: 'filter-status-popup',
  templateUrl: './payment-posting-list-filter-status-popup.component.html',
  styleUrls: ['./payment-posting-list-filter-status-popup.component.css']
})
export class PaymentPostingListFilterStatusPopupComponent {
  @Input() anchor: ElementRef;
  @Output() statusClickedEmmiter = new EventEmitter<any>();
  @Output() anchorViewportLeave = new EventEmitter();
  @Input() selectedStatuses: string[] = [];
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;

  @HostListener('document:click', ['$event'])
  public documentClick(event: any): void {
    if (!this.contains(event.target)) {
      this.anchorViewportLeave.emit();
    }
  }

  private unsubscribe = new Subject();
  reconcileStatuses: any[] = [];
  totalCount: number = 0;
  isLoading: boolean = false;

  constructor(private paymentPostingService: PaymentPostingService) {
    this.searchStatuses();
  }

  searchStatuses(): void {
    this.isLoading = true;
    this.paymentPostingService.getReconcileStatuses().subscribe(result => {
      if (result) {

        result.forEach(ele => this.reconcileStatuses.push({statusName: ele, checked: false}));

        this.reconcileStatuses = this.reconcileStatuses.where((x: any) =>
          !this.selectedStatuses.any((p: any) => p.statusName === x.statusName));
        this.reconcileStatuses = this.selectedStatuses.concat(this.reconcileStatuses);

        this.totalCount = result.length;
        this.isLoading = false;
      }
      return result;
    });
  }

  statusClicked(status) {
    this.statusClickedEmmiter.emit(status);
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
