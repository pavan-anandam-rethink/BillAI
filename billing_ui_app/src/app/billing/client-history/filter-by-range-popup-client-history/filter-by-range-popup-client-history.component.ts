import { Component, EventEmitter, OnDestroy, Output } from "@angular/core";
import { Subject } from "rxjs";



@Component({
    selector: 'app-filter-by-range-popup-client-history',
    templateUrl: './filter-by-range-popup-client-history.component.html',
    styleUrls: ['./filter-by-range-popup-client-history.component.css'],
})

export class FilterByRangePopupClientHistoryComponent implements OnDestroy {
    @Output() valueChanged = new EventEmitter();
    private unsubscribeAll$ = new Subject();

    patientResponsibilityFrom: number | undefined;
    patientResponsibilityTo: number | undefined;
    setValue(){
        let range = {
            From: this.patientResponsibilityFrom,
            To: this.patientResponsibilityTo
        };
        this.valueChanged.emit(range);
    }
    ngOnDestroy(): void {
        this.unsubscribeAll$.complete();
    }
}