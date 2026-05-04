import { Component, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild, ViewEncapsulation, HostListener } from '@angular/core';
import { Subject } from "rxjs";
import { MultiViewCalendarComponent } from "@progress/kendo-angular-dateinputs/calendar/multiview-calendar.component";
import { Helper } from '@app/billing/encounters/common/common-helper';

@Component({
    selector: 'calendar-popup',
    templateUrl: './calendar-popup.component.html',
    styleUrls: ['./calendar-popup.component.css'],
    encapsulation: ViewEncapsulation.None
})

export class CalendarPopupComponent implements OnInit, OnDestroy{
    @Input() anchor: ElementRef;
    @Input() range = {
        start: new Date(),
        end: new Date()
    };
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
    
    // public range = {
    //     start: new Date(),
    //     end: new Date()
    // };
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
        if(this.range.start == undefined) this.range.start = new Date();
        if(this.range.end == undefined) this.range.end = new Date();
       
        let selectedPeriod = {
            start: new Date(this.range.start),
            end: new Date(this.range.end)
        };

        this.selectedDateEmitter.emit(selectedPeriod);
        
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
        if (this.range.start){
            this.calendarTitle = new Date(this.range.start);
            this.updateTitle();
        }        
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