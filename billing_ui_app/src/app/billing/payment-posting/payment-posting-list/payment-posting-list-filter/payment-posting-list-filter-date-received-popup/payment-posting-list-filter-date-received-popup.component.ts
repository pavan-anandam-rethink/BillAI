import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from '@angular/core';
import { Subject } from 'rxjs';

@Component({
  selector: 'date-received-popup',
  templateUrl: './payment-posting-list-filter-date-received-popup.component.html',
  styleUrls: ['./payment-posting-list-filter-date-received-popup.component.css']
})
export class PaymentPostingListFilterDateReceivedPopupComponent {
  @Input() anchor: ElementRef;
@Input() childAnchor: ElementRef;
  @Output() optionClickedEmmiter = new EventEmitter<any>();
  @Output() anchorViewportLeave = new EventEmitter();
  @Input() selectedOption: string;
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;
  receivedDateOptions: string[] = ['All time', 'Last 7 days', 'Last 30 days', 'This month',
    'Last month', 'Custom Date Range'];
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

  constructor() {
    
  }

  statusClicked(status) {
    this.optionClickedEmmiter.emit(status);
  }
  private contains(target: any): boolean {
    return this.anchor.nativeElement.contains(target) || this.childAnchor?.nativeElement.contains(target) ||
      (this.popup ? this.popup.nativeElement.contains(target) : false);
  }

  ngOnDestroy() {
    this.unsubscribe.next(void 0);
    this.unsubscribe.complete();
  }

  ngOnInit() {
  }
}
