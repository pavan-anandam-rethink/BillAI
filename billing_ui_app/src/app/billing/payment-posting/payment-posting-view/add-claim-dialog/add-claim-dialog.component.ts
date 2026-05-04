import {
    Component, EventEmitter,
    OnDestroy, Input, Output, ViewChild,
    ViewEncapsulation,
    OnInit
} from '@angular/core';

import {ClaimPostingService, ClaimService} from '@core/services/billing';
import {
    CreatePaymentEraClaims, PaymentClaimsSearch
} from '@core/models/billing';

import { Observable, Subject } from "rxjs";
import { PaymentClaimsSearchBase } from "@core/models/billing/payment-claims-search";
import {debounceTime, distinctUntilChanged, takeUntil} from "rxjs/operators";
import { AccountMemberService } from '@core/services/account/account-member.service';


@Component({
    selector: 'add-claim-dialog',
    templateUrl: './add-claim-dialog.component.html',
    styleUrls: ['./add-claim-dialog.component.css'],
    encapsulation: ViewEncapsulation.None
})

export class AddClaimDialogComponent implements OnInit, OnDestroy{
    @Input() paymentId: number;
    @Input() isManual: boolean;
    @Output() closeDialogEmitter = new EventEmitter<CreatePaymentEraClaims>();
    @ViewChild("claimsAutocomplete") claimsAutocomplete: any;
    private unsubscribeAll$ = new Subject();
    isLoading: boolean;
    
    private delayedCall: any;
    
    isAddClaimBtnDisabled = false;
    
    public claimSearchRequest = new PaymentClaimsSearchBase();
    private claimSearchResponse: Observable<PaymentClaimsSearch[]>;
    claims: PaymentClaimsSearch[] = [];
    selectedClaims: PaymentClaimsSearch[] = [];
    subject: Subject<any> = new Subject();

    constructor(private claimService: ClaimService, private claimPostingService: ClaimPostingService, private accountService: AccountMemberService) {
        this.claimSearchResponse = this.claimService.getAccountClaims(this.claimSearchRequest, false);
    }

    ngOnInit(): void {
        this.subject
        .pipe(debounceTime(1000),distinctUntilChanged())
        .subscribe((e) => {
            this.getClaimsByValue(e);
            }
        );
    } 

    claimsSearchValueChange(event: any) {
        this.subject.next(event);
    }

    getClaimsByValue(event: any): void {        
        let newVal = event;
        this.delayedCall = window.setTimeout(() => {
            if(newVal == this.claimSearchRequest.searchString){
                this.searchClaim();
            } else {
                this.isLoading = false;
            }
        }, 900);
        
        this.claimSearchRequest.searchString = newVal;
        this.claimSearchRequest.paymentId = this.paymentId;
        this.claimSearchRequest.accountInfoId = this.accountService.memberDetails.accountInfoId;
    }
    
    searchClaim(): void {
        this.isLoading = true;
        this.claimService.getAccountClaims(this.claimSearchRequest, false)
            .subscribe(result => {
                let newClaim: PaymentClaimsSearch[] = [];
                this.selectedClaims.forEach(selectedClaim => {
                    let resultClaim = result.find(x => x.id == selectedClaim.id);
                    if(resultClaim !== undefined){
                        result.remove(resultClaim);
                    }
                    newClaim.push(selectedClaim);
                })
                newClaim = newClaim.concat(result);
                this.isLoading = false;
                return this.claims = newClaim;
            });
    }

    claimClick(event: any, selectedClaim: PaymentClaimsSearch): void {
        let claim = this.claims.find(x => x.id == selectedClaim.id)!;
        
        /*
        if(claim == undefined || (claim.checked == undefined && selectedClaim.checked == true) ||
            (claim.checked !== undefined && claim.checked != selectedClaim.checked)) {
            return;
        }
        
        claim.checked = claim.checked ? false : true;*/
        
        if(claim.checked){
            this.selectedClaims.remove(claim);
            claim.checked = false;
            //this.claims.remove(claim);
           
        }else{
            this.selectedClaims.push(claim);
            claim.checked = true;
            //this.claims.remove(claim);
            //this.claims.push(claim);
        }
        //this.claims = this.selectedClaims.concat(this.claims);
        //this.patientAutocomplete.reset();
        event.stopPropagation();
    }
    
    uncheckClaim(selectedClaimId: string): void {
        let claim = this.claims.find(x => x.id == selectedClaimId);
        if(claim == undefined){
            return;
        }

        claim.checked = claim.checked ? false : true;
        this.selectedClaims.remove(claim);
        if(this.selectedClaims.length == 0){
            this.claimsAutocomplete.reset();
        }
        
        this.claims.remove(claim);
        this.claims.push(claim);
    }

    onCloseAutocomplete(event: any) {
        event.preventDefault();
        
        //Close the list if the component is no longer focused
        setTimeout(() => {
            if(!this.claimsAutocomplete.wrapper.contains(document.activeElement)) {
                this.claimsAutocomplete.toggle(false);
            }
        });
    }

    onCloseDialog(claimsCount: CreatePaymentEraClaims = null): void {
        this.closeDialogEmitter.emit(claimsCount);
    }

    addClaims(): void {
        if(this.selectedClaims.length == 0) {
            return;
        }
        
        this.isAddClaimBtnDisabled = true;
        let claimsIds: string[] = [];
        this.selectedClaims.forEach(x => claimsIds.push(x.id));
        
        let model: CreatePaymentEraClaims = {
            paymentId: this.paymentId,
            claimsIds: claimsIds
        };
        this.onCloseDialog(model);
    }

    ngOnDestroy() {
        this.unsubscribeAll$.next(void 0);
        this.unsubscribeAll$.complete();
    }
}