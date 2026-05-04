import { Component, ElementRef, EventEmitter, HostListener, Input, OnDestroy, OnInit, Output, ViewChild, ViewEncapsulation } from '@angular/core';
import { Subject } from "rxjs";

@Component({
    selector: 'received-calendar-popup',
    templateUrl: './received-calendar-popup.component.html',
    styleUrls: ['./received-calendar-popup.component.css'],
    encapsulation: ViewEncapsulation.None
})

export class ReceivedCalendarPopupComponent implements OnInit, OnDestroy {
    @Input() anchor: ElementRef;
    @Input() parentAnchor: ElementRef;
    @Input() margin: any;
    @Input() receivedStartDate: any;
    @Input() receivedEndDate: any;
    @Input() range = {
            start: new Date(),
            end: new Date()
        };
    @Output() selectedDateEmitter = new EventEmitter();
    @Output() openPopupEmitter = new EventEmitter<boolean>();
    @Output() anchorViewportLeave = new EventEmitter();
    @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;
    private unsubscribe = new Subject();

    @HostListener('document:click', ['$event'])
    public documentClick(event: any): void {
        if (!this.contains(event.target)) {
            this.openPopupEmitter.emit(false);
        }
    }

    private contains(target: any): boolean {
        return this.anchor.nativeElement.contains(target) || this.parentAnchor?.nativeElement.contains(target) ||
            (this.popup ? this.popup.nativeElement.contains(target) : false);
    }

    private monthNames: string[] = ["January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    public isStartEdit: boolean = true;

    calendarTitle: Date = new Date(Date.now());
    calendarTitleString: string = `${this.monthNames[this.calendarTitle.getMonth()]}, ${this.calendarTitle.getFullYear()}`;

    onClickedOutside(_: Event) {
        this.emitClose();
    }

    monthChanged(param: any): void {
        let currentMonth = this.calendarTitle.getMonth();
        switch (param) {
            case 0:
                this.calendarTitle.setMonth(currentMonth - 1);
                break;
            case 1:
                this.calendarTitle.setMonth(currentMonth + 1);
                break;
            default:
                let inputDate = param as Date;
                if (inputDate != null) {
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
        let requestDate = {
            start: this.range.start.toDateString(),
            end: this.range.end.toDateString()
        };

        this.selectedDateEmitter.emit(requestDate);
        this.emitClose();
    }

    emitClose(): void {
        this.openPopupEmitter.emit(false);
    }

    ngOnInit(): void {
        if (!(this.receivedStartDate == undefined && this.receivedStartDate == null)) {
            this.range.start = new Date(this.receivedStartDate);
            this.range.end = new Date(this.receivedEndDate);

            this.calendarTitle = new Date(this.range.start);
            this.updateTitle();
        }
    }

    ngOnDestroy() {
        this.unsubscribe.next(void 0);
        this.unsubscribe.complete();
    }

    clickNext() {
        (<HTMLElement>document.querySelector(".date-filter-selector .k-calendar-header .k-calendar-nav-next"))!.click()
        this.monthChanged(1);
    }

    clickPrev() {
        (<HTMLElement>document.querySelector(".date-filter-selector .k-calendar-header .k-calendar-nav-prev"))!.click()
        this.monthChanged(0);
    }
}