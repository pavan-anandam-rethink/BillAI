import { Component, OnDestroy } from '@angular/core';
import { combineLatest, Subject } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { map, takeUntil } from 'rxjs/operators';import { ClaimPostingService, ClaimService, PaymentPostingService } from "@core/services/billing";
import { GridDataResult, PageChangeEvent, PagerSettings } from "@progress/kendo-angular-grid";
import { PatientDetailsComponent } from "@app/billing/payment-posting/payment-posting-view/payment-details/patient-details";
import { SidebarService } from "@app/shared/components/sidebar";
import { ClaimEOBInfo, PaymentClaimServiceLine, PaymentEOBInfo } from "@core/models/billing";
import { SortDescriptor } from "@progress/kendo-data-query";
import { IdFilterSort } from "@core/models/billing/id-filter-sort";
import * as FileSaver from 'file-saver';
import { AccountMemberService } from '@core/services/account/account-member.service';


@Component({
    selector: 'errors',
    templateUrl: './errors.html',
    styleUrls: ['./errors.css']
})
export class ErrorsComponent implements OnDestroy {

    private unsubscribeAll$ = new Subject<void>();

    paymentId: number;
    gridState: IdFilterSort = new IdFilterSort();

    public mySelection: number[] = [];

    claims: ClaimEOBInfo[];
    payment: PaymentEOBInfo;

    view: GridDataResult = {
        data: [],
        total: 0
    };

    readonly pagingSettings: PagerSettings = {
        buttonCount: 5,
        type: 'numeric',
        pageSizes: true,
        previousNext: true
    };

    gridPageSizes: any;
    constructor(private route: ActivatedRoute,
        private claimPostingService: ClaimPostingService,
        private sidebarService: SidebarService,
        private paymentPostingService: PaymentPostingService,
        private claimsService: ClaimService,
        private accountService: AccountMemberService) {
        this.route.params.pipe(
            takeUntil(this.unsubscribeAll$))
            .subscribe(x => {
                if (x["id"]) {
                    this.paymentId = +x["id"];
                    //this.loadErrorClaims();
                }
            });

        this.getGridPageSizes();
    }

    loadEOBInfo() {
        let payment$ = this.paymentPostingService.getEOBInfoById(this.paymentId).pipe(takeUntil(this.unsubscribeAll$));
        let claims$ = this.claimPostingService.getSelectedEOBClaims(this.paymentId, this.mySelection).pipe(map(claims => {
            claims.map(claim => {
                // claim.minDOS = claim.serviceLines.min((sl: PaymentClaimServiceLine) => sl.dateOfService);
                // claim.maxDOS = claim.serviceLines.max((sl: PaymentClaimServiceLine) => sl.dateOfService);

                return claim;
            });
            return claims;
        }));

        combineLatest(payment$, claims$).subscribe(value => {
            this.payment = value[0];
            this.claims = value[1];

            setTimeout(window.print, 100);
        })
    }


    loadErrorClaims() {
        this.gridState.Id = this.paymentId;
        this.gridState.MemberId = this.accountService.memberDetails.memberId;
        this.gridState.AccountInfoId = this.accountService.memberDetails.accountInfoId ;
        this.claimPostingService.getErrorClaims(this.gridState)
            .pipe(takeUntil(this.unsubscribeAll$))
            .subscribe((x: any) => {
                this.view.data = x.data; this.view.total = x.totalCount;
            });
    }

    showClientInfo(patientId: number) {
        this.sidebarService.openRight(PatientDetailsComponent, true, "md").subscribe(rsidebarRef =>
            rsidebarRef.instance.setData(patientId)
        );
    }

    onSortChange(sortParams: SortDescriptor[]): void {
        this.gridState.sortingModels = sortParams;
        this.loadErrorClaims();
    }

    onPageChange(event: PageChangeEvent): void {
        this.gridState.skip = event.skip;
        this.gridState.take = event.take;

        if(this.gridState.take === 0) this.gridState.take = this.view.total;
        this.loadErrorClaims();
    }

    print() {
        //this.loadEOBInfo();
        this.loadEOBPDF(true);
    }
    
    download(){
        this.loadEOBPDF();
    }
    
    loadEOBPDF(printDoc = false){
        const currentUserDateTime = new Date();
        this.claimPostingService.getEOBPdf(this.paymentId, this.mySelection, currentUserDateTime, true)
            .pipe(takeUntil(this.unsubscribeAll$))
            .subscribe((result:any) => {
                this.processPDF(result, printDoc);                    
            })
    }
    
    private processPDF(response: any, printDoc = false){
        let contentDispositionHeader = response.headers.get('Content-Disposition');
        if (contentDispositionHeader == undefined)
            return "";

        let name = contentDispositionHeader.split(';')[1].trim().split('=')[1];
        name = name.replace(/"/g, '');
        const filename = `${name}.pdf`

        const blob = new Blob([response.body], { type: 'application/pdf' });
        
        if(printDoc){
            const blobUrl = URL.createObjectURL(blob);
            const iframe = document.createElement('iframe');
            document.body.appendChild(iframe);

            iframe.style.display = 'none';
            iframe.src = blobUrl;
            iframe.onload = function(){
                window.setTimeout(function(){
                    iframe.focus();
                    iframe.contentWindow!.print();
                }, 100);
            }
        }else{
            FileSaver(blob, filename);
        }
    }

    changeSelection(selected: number[]) {
        this.mySelection = selected;
    }

    ngOnDestroy(): void {
        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

     getGridPageSizes(): void {
        const storedGridPageSizes = localStorage.getItem('gridPageSizes') ? JSON.parse(localStorage.getItem('gridPageSizes') || '') : null;
        if (storedGridPageSizes) {
            this.gridPageSizes = storedGridPageSizes;
        } else {
            this.claimsService.getGridPageSizes().subscribe((sizes: Array<number | { text: string; value: number }>) => {
                this.gridPageSizes = sizes;
            });
        }
    }
}