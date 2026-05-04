import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from '@angular/core';
import { Helper } from '@app/billing/encounters/common/common-helper';
import { MultiViewCalendarComponent } from '@progress/kendo-angular-dateinputs';
import { Subject } from 'rxjs';

@Component({
  selector: 'accounts-receivables-calendar-popup',
  templateUrl: './accounts-receivables-calendar-popup.component.html',
  styleUrls: ['./accounts-receivables-calendar-popup.component.css']
})
export class AccountsReceivablesCalendarPopupComponent {
  @Input() anchor: ElementRef;
  @Input() DateValue:Date;
  @Output() selectedDateEmitter = new EventEmitter();
  @Output() anchorViewportLeave = new EventEmitter();
  @Output() onFilterLeave = new EventEmitter<boolean>();
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;
  @ViewChild('calenderComponent') calendar: MultiViewCalendarComponent;
  
  @HostListener('document:click', ['$event'])
  public documentClick(event: any): void {
      if (!this.contains(event.target)) {
          this.anchorViewportLeave.emit();
          this.onFilterLeave.emit(false);
      }
  }

  private contains(target: any): boolean {
      return (this.anchor ? this.anchor.nativeElement.contains(target) : true) ||
          (this.popup ? this.popup.nativeElement.contains(target) : false);
  }

  private unsubscribe = new Subject<void>();

  private monthNames: string[] = ["January", "February", "March", "April", "May", "June",
      "July", "August", "September", "October", "November", "December"
  ];
  
 
  calendarTitle: Date = new Date(Date.now());
  calendarTitleString: string = `${this.monthNames[this.calendarTitle.getMonth()]}, ${this.calendarTitle.getFullYear()}`;
  
  monthChanged(param: any):void  {
      let currentMonth = this.calendarTitle.getMonth();
      switch (param){
          case 0:
              this.calendarTitle.setMonth(currentMonth-1);
              break;
          case 1:
              this.calendarTitle.setMonth(currentMonth+1);
              break;
          default:
              let inputDate = param as Date;
              if(inputDate != null){
                  this.calendarTitle.setMonth(inputDate.getMonth());
                  this.calendarTitle.setFullYear(inputDate.getFullYear());
              }
              break;
      }
      this.updateTitle();
      
  }

  updateTitle(): void {
      this.calendarTitleString = `${this.monthNames[this.calendarTitle.getMonth()]}, ${this.calendarTitle.getFullYear()}`;
  }
  
  emitDate(): void {

      this.selectedDateEmitter.emit(new Date(this.DateValue));
      
      this.emitClose();
  }
  
  emitClose(): void {
      this.onFilterLeave.emit(false);
  }
  
  ngOnDestroy() {
      this.unsubscribe.next();
      this.unsubscribe.complete();
  }
  
  ngOnInit() {
  }

  clickNext(){
      (<HTMLElement>document.querySelector(".date-filter-selector .k-calendar-header .k-calendar-nav-next"))!.click()
      this.monthChanged(1);
  }

  clickPrev(){
      (<HTMLElement>document.querySelector(".date-filter-selector .k-calendar-header .k-calendar-nav-prev"))!.click()
      this.monthChanged(0);
  }
}
