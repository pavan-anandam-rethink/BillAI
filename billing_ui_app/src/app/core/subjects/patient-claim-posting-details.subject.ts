import {ClaimDetailsListFilterSort, ClaimPostingDetails, PatientClaimDetailsFilterSort} from '@core/models/billing';
import { ClaimPostingService } from '@core/services/billing';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';


export class PatientClaimPostingDetailsSubject extends BaseBehaviorSubject<ClaimPostingDetails> {
    private loadLinkedData$ = new Subject<PatientClaimDetailsFilterSort>();
    private loadUnlinkedData$ = new Subject<PatientClaimDetailsFilterSort>();
    public totalCount = 0;

    constructor(private claimPostingService: ClaimPostingService) {
        super();
        this.loadLinkedData$.pipe(
            switchMap((params: PatientClaimDetailsFilterSort) => this.claimPostingService.getPatientPaymentClaimLinkedServiceLines(params).pipe(
                catchError(err => {
                    console.error(err);
                    return [];
                })
            ))
        ).subscribe(result => {
            this.totalCount = result.totalCount
            this.data = result.data;
            this.data.forEach(ele => ele.checked = false);
            this.sync();
        });
        this.loadUnlinkedData$.pipe(
            switchMap((params: PatientClaimDetailsFilterSort) => this.claimPostingService.getPatientPaymentClaimUnlinkedServiceLines(params).pipe(
                catchError(err => {
                    console.error(err);
                    return [];
                })
            ))
        ).subscribe(result => {
            this.totalCount = result.totalCount
            this.data = result.data;
            this.data.forEach(ele => ele.checked = false);
            this.sync();
        });
    }

    getAllLinked(params: PatientClaimDetailsFilterSort) {
        this.loadLinkedData$.next(params);
    }
    getAllUnlinked(params: PatientClaimDetailsFilterSort) {
        this.loadUnlinkedData$.next(params);
    }

    updateServiceLine(id: number){
        this.claimPostingService.getPaymentClaimServiceLine(id).subscribe(x => {
            let serviceLine = this.data.find(y => y.id == id);
            if(serviceLine != undefined){

                serviceLine.patientPayment = x.serviceLinePaymentAmount;
                serviceLine.adjustment = x.adjustment;
                serviceLine.patientResponsibility = x.patientResponsibility;
                serviceLine.patientResponsibilityBalance = x.patientResponsibilityBalance;
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

    getPatientId(id: number){
        let serviceLine = this.data.find(y => y.id == id);
        if(serviceLine != undefined){
            return serviceLine.patientId;
        }

        return undefined;
    }
}