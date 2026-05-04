import {ClaimPosting, ClaimListFilterSort, RemovePaymentClaims} from '@core/models/billing';
import { ClaimPostingService } from '@core/services/billing';
import { BaseBehaviorSubject } from './base.behavior.subject';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';


export class PatientClaimPostingListSubject extends BaseBehaviorSubject<ClaimPosting> {

    private loadData$ = new Subject<ClaimListFilterSort>();
    public loading$ = new BehaviorSubject<boolean>(false);
    public totalCount = 0;

    constructor(private claimPostingService: ClaimPostingService) {
        super();

        this.loading$.next(true);
        this.loadData$.pipe(
            switchMap((params: ClaimListFilterSort) => this.claimPostingService.getAllForPatients(params).pipe(
                catchError(err => {
                    console.error(err);
                    return [];
                })
            ))
        ).subscribe(result => {
            //this.totalCount = result.totalCount
            this.data = result.data;
            this.data.forEach(ele => {
                ele.checked = false;
                ele.linkedchecked = false;
            })
            this.totalCount = result.totalCount;
            this.loading$.next(false);
            this.sync();
        });
    }

    getAll(params: ClaimListFilterSort) {
        this.loadData$.next(params);
    }
    
    getLoading(): Observable<boolean> {
        return this.loading$;
    }

    setLoading() {
        this.loading$.next(true);
    }

    delete(claim: ClaimPosting) {
        if (claim.id > 0) {
            this.claimPostingService.deleteById(claim.id).subscribe(result => {
                this.remove(result);
                this.sync();
            });
        } else {
            this.remove(claim);
            this.sync();
        }
    }

    updateClaim(data: any){
        if (data != null && data.claimId > 0) {
            this.claimPostingService.getClaimPostingDetails(data.claimId).subscribe(x => {
                let claim = this.data.find(x => x.patientId == data.patientId);
                if(claim != undefined) {
                    claim.paidAmount = x.paidAmount;
                    claim.patientResponsibility = x.patientResponsibility;
                    claim.allowedAmount = x.allowedAmount;
                    claim.billedAmount = x.billedAmount;
                    claim.balance = x.patientResponsibilityBalance;
                    this.sync();
                }
            })
        } 
    }

    deleteSelected(model: RemovePaymentClaims) {
        if(model && model.paymentClaimsIds.length > 0){
            this.claimPostingService.deleteSelectedClaims(model);
        }else{
            this.sync();
        }
    }
}