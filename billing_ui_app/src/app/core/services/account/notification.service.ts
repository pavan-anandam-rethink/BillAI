import { Injectable } from "@angular/core";
import { Authorization } from "@core/models/clients/authorization";
import Pusher, { Channel } from "pusher-js";
import { BehaviorSubject, Observable, Subject } from "rxjs";
import { environment } from "src/environments/environment";

export interface ClaimStatusUpdate {
    claimId: number;
    success: string;
    total: number;
    batchId: string;
}

@Injectable({
    providedIn: 'root',
})

export class NotificationService {
    private apiBaseUrl = environment.claimApiBaseUrl;
    private key = environment.key;
    private cluster = environment.cluster;
    private pusher: Pusher;
    private channel: Channel;
    private connected = false;

    // Persistent notification state that survives component re-creation
    notifications: any[] = [];

    private claimUpdatesSubject: Subject<ClaimStatusUpdate> = new Subject<ClaimStatusUpdate>();
    claimUpdates$: Observable<ClaimStatusUpdate> = this.claimUpdatesSubject.asObservable();


  // START: Reports Page Tab Index Observable Section
    private tabIndexSubject:BehaviorSubject<number>=new BehaviorSubject<number>(0);
    tabIndex$=this.tabIndexSubject.asObservable();

    setTabIndex(index:number){
        this.tabIndexSubject.next(index);
    }
  // ENDS: Reports Page Tab Index Observable Section

    


    // This section is failure notification related
    private encounterIdSource = new Subject<number>();
    encounterId$ = this.encounterIdSource.asObservable();
    
    sendEncounterId(id: number) {
        this.encounterIdSource.next(id);
    }

    // This section is failure notification related
    private viewFailedIdsSource = new Subject<number[]>();
    viewFailedIds$ = this.viewFailedIdsSource.asObservable();
    
    viewFailedEncounters(ids: number[]) {
        this.viewFailedIdsSource.next(ids);
    }

    // Share Success Processed claimIds
    private successClaimId = new Subject<number>();
    successClaimId$ = this.successClaimId.asObservable();

    sendSuccessClaimId(id: number) {
        this.successClaimId.next(id);
    }

    //Pending Review 
    private encounterIdSourcePendingReview = new Subject<number>();
    encounterIdPendingReview$ = this.encounterIdSourcePendingReview.asObservable();
    
    sendEncounterIdPendingReview(id: number) {
        this.encounterIdSourcePendingReview.next(id);
    }
    // This section is failure notification related-Pending Review
    private failureClaimId = new Subject<number[]>();
    failureClaimId$ = this.failureClaimId.asObservable();
    
    sendFailureClaimId(ids: number[]) {
        this.failureClaimId.next(ids);
    }

    //Pending Review Processed claimIds-Success
    private pendingReviewClaimId = new Subject<number>();
    pendingReviewClaimId$ = this.pendingReviewClaimId.asObservable();

    // End of failure notification related section

    connect(accountId: string, userId: string) {
        if (this.connected) return;
        this.connected = true;

        this.pusher = new Pusher(this.key, {
            cluster: this.cluster,
            authEndpoint: `${this.apiBaseUrl}/Pusher/auth`,
            auth: {
                headers: {
                    Authorization: `Bearer ${localStorage.getItem('token')}`
                }
            }     
        });

        // Subscribe to account-specific channel
        this.channel = this.pusher.subscribe(`private-account-${accountId}-user-${userId}`);

        // Bind to event
        this.channel.bind(`claim-status-updated`, (data: ClaimStatusUpdate) => {
            this.claimUpdatesSubject.next(data);
        });
    }

    disconnect(){
        if (this.channel && this.pusher) {
            this.pusher.unsubscribe(this.channel.name);
            this.connected = false;
        }
    }
}
