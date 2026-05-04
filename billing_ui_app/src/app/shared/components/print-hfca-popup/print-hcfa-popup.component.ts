import { Component, Input, OnDestroy, ElementRef, ViewChild, HostListener, Output, EventEmitter } from "@angular/core";
import { AccountPermissions } from "@core/enums/account/account-permissions";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { Subject, Subscription } from "rxjs";

@Component({
    selector: 'print-hcfa-popup',
    templateUrl: './print-hcfa-popup.component.html',
    styleUrls: ['./print-hcfa-popup.component.css'],
})

export class PrintHCFAPopupComponent implements OnDestroy {
    @Input() anchor: ElementRef;
    @Input() isPatientInvoiceComponent : boolean;
    @Output() onPrint = new EventEmitter();
    @Output() onPrintMarkAsBilledOrSubmitted = new EventEmitter();
    @Output() onPrintPopupLeave = new EventEmitter<boolean>();
    @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;

    subscriptions = new Subscription();
    canSubmitClaim = false;
    canSubmitInvoice = false;

    constructor(
        private accountService: AccountMemberService,
    ) {
        this.subscriptions.add(this.accountService.accountMemberSettings.subscribe((x) => {
            if (x) {
                this.canSubmitClaim = this.accountService.checkPermissionLevel(AccountPermissions.BillingSubmitClaims);
                this.canSubmitInvoice = this.accountService.checkPermissionLevel(AccountPermissions.BillingReopenEncounter);
            }
        }));
    }

    @HostListener('document:click', ['$event'])
    public documentClick(event: any): void {
        if (!this.contains(event.target)) {
            this.onPrintPopupLeave.emit(false);
        }
    }

    private unsubscribe = new Subject();

    private contains(target: any): boolean {
        return this.anchor.nativeElement.contains(target) ||
            (this.popup ? this.popup.nativeElement.contains(target) : false);
    }

    print() {
        this.onPrint.emit();
        this.onPrintPopupLeave.emit(false);
    }

    printMarkAsBilledOrSubmitted() {
        this.onPrintMarkAsBilledOrSubmitted.emit();
        this.onPrintPopupLeave.emit(false);
    }

    ngOnDestroy(): void {
        this.unsubscribe.next(void 0);
        this.unsubscribe.complete();
    }

}