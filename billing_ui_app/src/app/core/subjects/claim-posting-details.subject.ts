import { ClaimDetailsListFilterSort, ClaimPostingDetails } from '@core/models/billing';
import { ClaimPostingService } from '@core/services/billing';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';


export class ClaimPostingDetailsSubject extends BaseBehaviorSubject<ClaimPostingDetails> {
    private loadData$ = new Subject<ClaimDetailsListFilterSort>();
    public totalCount = 0;

    constructor(private claimPostingService: ClaimPostingService) {
        super();
        this.loadData$.pipe(
            switchMap((params: ClaimDetailsListFilterSort) => this.claimPostingService.getPaymentClaimServiceLines(params).pipe(
                catchError(err => {
                    console.error(err);
                    return [];
                })
            ))
        ).subscribe(result => {
            this.totalCount = result.totalCount
            this.data = result.data;
            this.sync();
        });
    }

    getAll(params: ClaimDetailsListFilterSort) {
        this.loadData$.next(params);
    }
    
    updateServiceLine(id: number){
        this.claimPostingService.getPaymentClaimServiceLine(id).subscribe(x => {
            let serviceLine = this.data.find(y => y.id == id);
            if(serviceLine != undefined){
                
                serviceLine.paidAmount = x.insurancePayment;
                serviceLine.adjustment = x.adjustment;
                serviceLine.patientResponsibility = x.patientResponsibility;
                serviceLine.allowedAmount = x.allowedAmount;
                serviceLine.balance = x.balance;
                this.sync();
            }
        })
    }
    
    getClaimId(id: number){
        let serviceLine = this.data.find(y => y.id == id);
        if(serviceLine != undefined){
            return serviceLine.claimId;
        }
        
        return undefined;
    }
}