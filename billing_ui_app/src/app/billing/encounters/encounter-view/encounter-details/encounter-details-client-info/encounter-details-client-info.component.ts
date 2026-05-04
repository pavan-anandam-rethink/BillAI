import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { DiagnosisCodeEditorComponent } from '@app/billing/encounters/common/diagnosis-editor/diagnosis-code-editor/diagnosis-code-editor.component';
import { SidebarService } from '@app/shared/components/sidebar';
import { AccountPermissions } from '@core/enums/account/account-permissions';
import { FunderType } from '@core/enums/clients';
import { ClaimDetailsInfoModel } from '@core/models/billing';
import { AuthorizationDiagnosisCode } from '@core/models/clients/authorization';
import { BasicOption } from '@core/models/common';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { ClientsService } from '@core/services/clients/clients.service';
import { PreventableEvent } from '@progress/kendo-angular-dropdowns';
import { Subject, takeUntil } from 'rxjs';

@Component({
    selector: 'app-encounter-details-client-info',
    templateUrl: './encounter-details-client-info.component.html',
    styleUrls: ['./encounter-details-client-info.component.css']
})
export class EncounterDetailsClientInfoComponent {
    @Input() claim: ClaimDetailsInfoModel;
    @Input() childProfileId: number;
    @Input() claimForm: FormGroup;
    @Output() onChangeDiagnosicCode= new EventEmitter<any[]>();
    public diagnosisCodesControl: FormControl = new FormControl([]);
    public funderIsInsurance = false;
    authorizations: BasicOption[];
    allowManualAuth = false;
    isAuthNumberDisabled = false;
    canEdit: boolean;

    private unsubscribeAll$ = new Subject<void>();
    
    constructor(private sidebarService: SidebarService, 
        private clientService: ClientsService, 
        private readonly router: Router,
        private accountService: AccountMemberService) {
            this.accountService
            .accountMemberSettings
            .subscribe((x) => {
                if (x) {
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEdit);
                }
            });
         }

    ngOnInit(): void {
        // this.getClientAuthorizationsForClaim();
        this.diagnosisCodesControl = this.claimForm.controls["diagnosisCodes"] as FormControl;
        this.funderIsInsurance = this.claim.funderTypeId == FunderType.Insurance;
        this.updateValidators();
    }

    getClientAuthorizationsForClaim() {
        this.clientService.getClientAuthorizationsForClaim(this.claim.patientId, this.claim.funderId, this.claim.serviceLineId, this.accountService.memberDetails.accountInfoId).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
            authorizations => {
                this.authorizations = authorizations;
            },
            error => {
                this.router.navigate(["/billing/claims"]);
            }
        );
    }

    

    updateValidators() {
        if (this.funderIsInsurance) {
            this.claimForm.controls['diagnosisCodes'].setValidators([Validators.required]);
        } else {
            this.claimForm.controls['diagnosisCodes'].setValidators([]);
        }

        this.claimForm.controls['diagnosisCodes'].updateValueAndValidity({ emitEvent: false, onlySelf: true });
    }

    get getSortedCodes(): AuthorizationDiagnosisCode[] {
        return this.diagnosisCodesControl.value.sort(function (a: AuthorizationDiagnosisCode, b: AuthorizationDiagnosisCode) {
            return a.order - b.order
        });
    }

    get getDiagnosisCodes() {
        return this.getSortedCodes.map(x => x.diagnosisCode).join(", ");
    }

    get showAuthorizationStatus(): boolean {
        return !!this.claim?.authorizationStatus && this.claim.authorizationStatus !== 'Yes';
    }

    get showAuthorizationWarning(): boolean {
        return this.claim?.authorizationStatus === 'NotNeeded';
    }

    get formattedAuthorizationStatus(): string {
        if (this.claim?.authorizationStatus === 'NotNeeded') {
            return 'Not Needed';
        }
        return this.claim?.authorizationStatus || '';
    }

    diagnosisCodeClick() {
        this.sidebarService.openRight(DiagnosisCodeEditorComponent, true, "md").subscribe(sidebar => {
            let instance: DiagnosisCodeEditorComponent = sidebar.instance;

            instance.loadData(this.diagnosisCodesControl.value);
            document.getElementById("AddButton")?.getElementsByTagName("div")[0]?.focus();
            instance.save.subscribe((x: AuthorizationDiagnosisCode[]) => {
                this.diagnosisCodesControl.patchValue(x);
                this.onChangeDiagnosicCode.emit(x);
                this.sidebarService.closeAll();
                document.getElementById("AddField")?.getElementsByTagName("input")[0]?.focus();
            })
            instance.cancel.subscribe((x: any) => {
                this.sidebarService.closeAll();
                document.getElementById("AddField")?.getElementsByTagName("input")[0]?.focus();

            })
        })
    }

    onAuthNumberChange(authId: number | string) {
        if (typeof authId === 'number') {
            // this.stepFormGroup.controls['allowManualAuthorization'].setValue(false);
        }
    }

    
    onAuthNumberOpen(event: PreventableEvent) {
        if (this.allowManualAuth) {
            event.preventDefault();
        }
    }
}
