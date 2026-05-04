import {
    Component,
    ElementRef, EventEmitter, HostListener,
    Input,
    OnDestroy,
    OnInit, Output, ViewChild,
} from '@angular/core';

import { PaymentPostingService } from '@core/services/billing';
import {
    PaymentPostingFunderSearch,
    PaymentPostingListFunderSearch,
    PaymentPostingListFunderSearchBase
} from '@core/models/billing';

import { Observable, Subject } from "rxjs";

@Component({
    selector: 'funder-search-popup',
    templateUrl: './funder-search-popup.component.html',
    styleUrls: ['./funder-search-popup.component.css'],
})

export class FunderSearchPopupComponent implements OnInit, OnDestroy {
    @Input() anchor: ElementRef;
    @Output() selectFundersEmitter = new EventEmitter<PaymentPostingListFunderSearch[]>();
    @Output() funderClickedEmmiter = new EventEmitter<PaymentPostingListFunderSearch>();
    @Output() anchorViewportLeave = new EventEmitter();
    @Input() selectedFunders: PaymentPostingListFunderSearch[] = [];
    @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;

    @HostListener('document:click', ['$event'])
    public documentClick(event: any): void {
        if (!this.contains(event.target)) {
            this.anchorViewportLeave.emit();
        }
    }
    
    private unsubscribe = new Subject();
    private delayedCall: any;
    
    funderSearchRequest: PaymentPostingListFunderSearchBase = new PaymentPostingListFunderSearchBase(10000);
    funderSearchResult: Observable<PaymentPostingFunderSearch>;
    funders: PaymentPostingListFunderSearch[] = [];
    totalCount: number = 0;
    
    isLoading: boolean = false;

    constructor(private paymentPostingService: PaymentPostingService) {
        this.searchFunders();
    }
    
    searchFunders():void {
        this.isLoading = true;
        this.paymentPostingService.getAssignedFunders(this.funderSearchRequest).subscribe(result => {
            if (result.funders){
                this.funders = result.funders.where((x: any) =>
                !this.selectedFunders.any((p: any) => p.funderName === x.funderName));
                this.funders = this.selectedFunders.concat(this.funders);
                this.totalCount = result.totalCount;
                this.isLoading = false;
            }
            return result;
        });
    }
    
    fundersSearchValueChanged(event: any): void {
        let newVal = event.target.value;
        
        this.delayedCall = window.setTimeout(() => {
            if(newVal == this.funderSearchRequest.funderName){
                this.searchFunders();
            }
        }, 1000);

        this.funderSearchRequest.funderName = newVal;      
    }
    
    funderClicked(funder: PaymentPostingListFunderSearch){
        this.funderClickedEmmiter.emit(funder);        
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