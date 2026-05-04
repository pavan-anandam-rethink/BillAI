import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from "@angular/core";
import { Subject } from "rxjs";

import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";

@Component({
    selector: 'claim-filters-validation-popup',
    templateUrl: './claim-filters-validation-popup.component.html',
    styleUrls: ['./claim-filters-validation-popup.component.css'],
})

export class ClaimFiltersValidationPopupComponent implements OnDestroy, OnInit {
    @Output() validationClicked = new EventEmitter<number>();
    @Input() selectedValidations: ClaimFilterOptionModel[];
    private unsubscribeAll$ = new Subject();

    totalCount: number;
    validations: ClaimFilterOptionModel[] = [];
    isLoading: boolean;

    searchTimeout: any;

    validationFilterOptions = [
        { id: 2, name: 'Errors'},
        { id: 3, name: 'Alerts'},
        { id: 4, name: 'Responses'},
        { id: 99, name: 'No Errors/Alerts'}
    ] as ClaimFilterOptionModel[];

    constructor() {
    }

    search() {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        this.searchTimeout = setTimeout(() => {
            this.isLoading = true;

            this.validations = this.selectedValidations;
            this.validations = this.validations.concat(this.validationFilterOptions.where((p: ClaimFilterOptionModel) =>
                !this.selectedValidations.any((s: ClaimFilterOptionModel) => s.id == p.id)))

            this.isLoading = false;
        });
    }

    onValidationClicked(item: ClaimFilterOptionModel) {
        if (item.checked) {
            this.selectedValidations.remove(item);
            item.checked = false;
        } else {
            this.selectedValidations.push(item);
            item.checked = true;
        }

        this.validationClicked.emit()
    }

    ngOnDestroy(): void {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        //this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

    ngOnInit(): void {
        this.search()
        this.validations = [...this.selectedValidations];
    }
}