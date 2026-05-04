import { Component, ViewChild, ChangeDetectorRef, ChangeDetectionStrategy, Output, EventEmitter, Renderer2 } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { FormGroup, FormBuilder, FormArray } from "@angular/forms";
import { Observable, Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { ClientsService } from "@core/services/clients/clients.service";
import { ClaimService } from "@core/services/billing";
import { DiagnosisCodeStepComponent } from "./diagnosis-code-step/diagnosis-code-step.component";
import { ProviderStepComponent } from "./provider-step/provider-step.component";
import { ClaimBillingCode, ClaimInfoModel, ClaimSaveRequestModel, DiagnosisCode, ProviderInfoModel } from "@core/models/billing/claim-save-model";
import { Helper } from "../common/common-helper";
import { ClientBillingCode } from "@core/models/billing/billing-code";
import { ClientReferringProviderForDropdown } from "@core/models/clients/referring-provider";
import { ClientRenderingProvider } from "@core/models/clients/rendering-provider";
import { CompanyAccountLocation } from "@core/models/company-account";
import { ClientFunderModel, FunderServiceLine } from "@core/models/company-account/funders/client-funder-model";
import { ClientOptionModel } from "@core/models/clients";
import { ClaimCreateInfoGetModel } from "@core/models/billing";
import { Authorization, AuthorizationDiagnosisCode } from "@core/models/clients/authorization";
import { NoAuthDialogComponent } from "../common/no-auth-dialog/no-auth-dialog.component";
import { MatDialog } from "@angular/material/dialog";
import { IdWithUserInfo } from "@core/models/billing/get-claim-by-identifier";
import { ComponentCanDeactivate } from "@core/guards/dirty-form-guard";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { ClaimValidationModel } from "@core/models/billing/claim-validation-model";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";
import { BackCancelService } from "@app/billing/services/back-cancel.service";
import { ConfirmDialog } from "@core/models/common";
import { ConfirmationDialogComponent } from "@app/shared/components/confirmation-dialog/confirmation-dialog.component";
import { makeAllControlsTouched } from "@core/common/mark-every-controls-as-touched";

@Component({
    selector: 'app-add-claim',
    templateUrl: './add-claim.component.html',
    styleUrls: ['./add-claim.component.css'],
    changeDetection: ChangeDetectionStrategy.Default
})
export class AddClaimComponent implements ComponentCanDeactivate {
    private unsubscribeAll$ = new Subject<void>();

    headerText = "Create New Claim";
    status = 'Processed';
    isClaimInfoValid = false;
    isClaimInfoNextClicked = false;
    isProviderValid = false;
    isProviderNextClicked = false;
    isDiagnosisCodeValid = false;
    isSavingCompleted = false;
    isIncompleteAuthOpened = false;

    clientId: number;
    facilityId: number;
    serviceLineId: number;
    authId: number | string;
    prevAuthId: number;
    prevServiceLineId: number;
    prevClientId: number;
    createdClaimId: number;

    clients: ClientOptionModel[];
    authDetails: Authorization | undefined;
    renderingProviders: ClientRenderingProvider[];
    serviceProviders: CompanyAccountLocation[];
    clientReferringProviders: ClientReferringProviderForDropdown[];

    selectedFunder: ClientFunderModel;
    selectedServiceLine: FunderServiceLine;
    diagnosisCodes: AuthorizationDiagnosisCode[];
    billingCodes: ClientBillingCode[];
    req: IdWithUserInfo;
    @Output() clientRemoveParent = new EventEmitter<number>();

    @ViewChild(DiagnosisCodeStepComponent) childRef: DiagnosisCodeStepComponent;
    @ViewChild(ProviderStepComponent) providerStepRef: ProviderStepComponent;
    @ViewChild("cancelConfirmationAlert") cancelConfirmationAlert: ConfirmationDialogComponent;
    public createClaimForm = new FormGroup({
        formData: this.fb.array([])
    });

    public cancelChangesConfirmation = new ConfirmDialog(false, "Warning",
        "Any changes you've made will not be saved. Are you sure you want to Cancel?");

    constructor(private router: Router, private fb: FormBuilder, private clientService: ClientsService, private claimsService: ClaimService, 
        private cd: ChangeDetectorRef, private notificationHandler: NotificationHandlerService,
        private dialog: MatDialog, private accountService: AccountMemberService,
        private renderer:Renderer2,private readonly backCancelService: BackCancelService,
        private readonly route: ActivatedRoute) {
            this.backCancelService.backCancelEmmiter.subscribe((allowBackCancel => {
                this.cancelButtonEventHandler(!allowBackCancel);
              }))
    }

    public cancelButtonEventHandler(isFormWasChanged: boolean): void {
        if (isFormWasChanged) {
          this.cancelChangesConfirmation.opened = true;
        } else {
          this.router.navigate(["/billing/claims"]);
        }
      }

      navigateToDashboard(): void {
        this.router.navigate(["/billing/claims/list"]);
    }


    checkList() {
        if (this.renderingProviders.length > 0 && this.authDetails) {
            const staffInList = this.renderingProviders.filter((x) => x.staffMemberId === this.authDetails!.renderingProviderId);
            if (staffInList.length === 0) {
                this.renderingProviders.push({
                    id: this.authDetails.renderingProviderId!, name: this.authDetails.renderingProviderName, staffMemberId: this.authDetails.renderingProviderId!
                });
            }
        }
    }

    proceed(tabName: string) {
        this.isIncompleteAuthOpened = false;
        for (let i = 0; i < document.querySelectorAll('.mat-tab-label-content').length; i++) {
            if ((<HTMLElement>document.querySelectorAll('.mat-tab-label-content')[i]).innerText.includes(tabName)) {
                (<HTMLElement>document.querySelectorAll('.mat-tab-label')[i]).click();
            }
        }
    }

    cancelIncompleteAuth() {
        this.isIncompleteAuthOpened = false;
    }



    onAuthNoChange(e) {

        if (this.authId !== undefined && typeof this.authId === 'number') {
            if (!this.authDetails || this.authId !== this.prevAuthId) {
                
                if (this.diagnosisCodes && this.diagnosisCodes.length > 0) {
                    this.diagnosisCodes = [];
                }

                this.prevAuthId = this.authId;
                this.clientService.getAuthorization(this.authId, this.clientId).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
                    (auth) => {
                        this.authDetails = auth;
                        this.diagnosisCodes = auth.diagnosisCodes.filter((code) => code.includeOnClaims);
                        this.billingCodes = this.billingCodes.filter((code) => this.authDetails!.billingCodes.any((authCode) => authCode.billingCodeId == code.billingCodeId));
                        
                    }, (error) => {
                        
                    });
            }
        } else {
            if (this.authDetails) {
                this.authDetails = undefined;
            }

            const clientChanged = this.clientId !== this.prevClientId;
            const serviceLineChanged = this.selectedServiceLine.serviceId !== this.prevServiceLineId;
            if (clientChanged || serviceLineChanged) {
                this.prevClientId = clientChanged ? this.clientId : this.prevClientId;
                this.prevServiceLineId = serviceLineChanged ? this.selectedServiceLine.serviceId : this.prevServiceLineId;
                this.loadDiagnosisCodes();
            }
        }
    }

    onCancel()
    {
        this.router.navigateByUrl('/billing/claims/list');
    }

    onCreateClaim() {
        if (this.authId === undefined) {
            const authNumberControl = this.createClaimForm.get('formData.0.authorizationNumberId');
            if (authNumberControl && !authNumberControl.value) {
                const dialogRef = this.dialog.open(NoAuthDialogComponent, {
                    width: '540px',
                });

                dialogRef.afterClosed().subscribe(result => {
                    if (result) this.saveClaim();
                });
            }
        } else {
            this.saveClaim();
        }
    }

    saveClaim() {
        // Validate manual billing provider form when "Other" is selected
        const secondStepFormForValidation = (this.createClaimForm.get("formData") as FormArray).at(1) as FormGroup;
        const billingProviderId = secondStepFormForValidation?.get('billingProviderId')?.value;
        if (billingProviderId === 0 && this.providerStepRef && !this.providerStepRef.manualBillingProviderForm.valid) {
            makeAllControlsTouched(this.providerStepRef.manualBillingProviderForm);
            return;
        }

        const firstStepForm = (this.createClaimForm.get("formData") as FormArray).at(0) as FormGroup;
        const secondStepForm = (this.createClaimForm.get("formData") as FormArray).at(1) as FormGroup;
        const thirdStepForm = (this.createClaimForm.get("formData") as FormArray).at(2) as FormGroup;

        const claimInfo = { ...firstStepForm.getRawValue() } as ClaimInfoModel;

        if (claimInfo.allowManualAuthorization) {
            claimInfo.authorizationNumber = (`${claimInfo.authorizationNumberId}`).trim().substring(0,Math.min(50,(`${claimInfo.authorizationNumberId}`).trim().length));
            claimInfo.authorizationNumberId = undefined;
            
        }

        claimInfo.clientFunderId = this.selectedFunder.id;
        claimInfo.funderId = this.selectedFunder.funderId;

        const provider = { ...secondStepForm.value } as ProviderInfoModel;

        const billingCodes = thirdStepForm.get('billingCodes')!.getRawValue();
        const billingCodesDates = (billingCodes.billingCodes as ClaimBillingCode[])
            .map((code) => new Date(code.individualDateOfService).getTime());

        provider.dateOfServiceStart = Helper.shiftDateToUTC(new Date(Math.min.apply(null, billingCodesDates)));
        provider.dateOfServiceEnd = Helper.shiftDateToUTC(new Date(Math.max.apply(null, billingCodesDates)));
        provider.billingProviderId = provider.billingProviderId === -1 ? null : provider.billingProviderId;

        // Get manual billing provider data if "Other" is selected
        const billingProviderOther = this.providerStepRef?.getBillingProviderOtherData() || null;
        // If "Other" is selected, set billingProviderId to 0
        if (billingProviderOther) {
            provider.billingProviderId = 0;
        }

        billingCodes.billingCodes.forEach(element => {
            element.modifier1 = element.modifier1 ? element.modifier1 : "";
            element.modifier2 = element.modifier2 ? element.modifier2 : "";
            element.modifier3 = element.modifier3 ? element.modifier3 : "";
            element.modifier4 = element.modifier4 ? element.modifier4 : "";
        });

        const diagnosisCode: DiagnosisCode = {
            diagnosisCodesToSave: thirdStepForm.get('diagnosisCodes')!.value,
            billingCodes: billingCodes.billingCodes,
        };
        diagnosisCode.billingCodes.forEach((x) => x.individualDateOfService = this.convertDate(x.individualDateOfService));

        const impersonationUserName = this.accountService.memberDetails.impersonationUserName
        const claimRequest: ClaimSaveRequestModel = {
            accountInfoId: this.accountService.memberDetails.accountInfoId,
            memberId: this.accountService.memberDetails.memberId,
            claim: { claimInfo, provider, diagnosisCode, impersonationUserName }
        }

        // Add billing provider request if "Other" is selected
        if (billingProviderOther) {
            claimRequest.billingProviderRequest = {
                claimId: 0, // Will be set by backend after claim is created
                billingProvider: billingProviderOther
            };
        }

        this.claimsService.saveClaim(claimRequest).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
            (x) => {
                this.createdClaimId = x;
                const validateModel: ClaimValidationModel = {
                                        Id : this.createdClaimId,
                                        isSecondary: false,
                                        secondaryFunderId: undefined,
                                        AccountInfoId: this.accountService.memberDetails.accountInfoId,
                                        MemberId: this.accountService.memberDetails.memberId
                                    };
                this.claimsService.ReRunValidation(validateModel, true).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
                    () => {
                        this.isSavingCompleted = true;
                        this.notificationHandler.showNotificationSuccess('Claim saved successfully');
                        this.router.navigate(['/billing/claims/list']);
                    });
            },
            error => {
                this.isSavingCompleted = true;
                
                this.notificationHandler.showNotificationError('Failed to save the claim!');
            }
        );
    }

    convertDate(individualDateOfService: Date) {
        var dateconv = individualDateOfService.getTime();
        var dateOfServiceStart = Helper.shiftDateToUTC(new Date(dateconv));
        return dateOfServiceStart;

    }
    loadDiagnosisCodes() {
        this.clientService.getDiagnosisForClaimWithoutAuth(this.clientId, this.selectedServiceLine.serviceId, this.accountService.memberDetails.accountInfoId).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
            x => {
                this.diagnosisCodes = x;
            }
        );
    }

    clientRemove(e) {
        this.clientId = 0;
        this.billingCodes = [];
    }

    ngOnInit() {
        
        this.clientService.getClientsForClaim({
            AccountInfoId:this.accountService.memberDetails.accountInfoId,
            MemberId:this.accountService.memberDetails.memberId
        }).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
            clients => {
                this.clients = clients.sort((c1, c2) => (c1.name > c2.name) ? 1 : -1);
                
                this.cd.detectChanges();
            }, (error: any) => {
                
            });
    }

    createClaimInfo(event) {
        
        if (this.clientId && this.selectedFunder && this.selectedServiceLine) {
            const getClaimInfoModel: ClaimCreateInfoGetModel = {
                clientId: this.clientId,
                funderId: this.selectedFunder.funderId,
                serviceId: this.selectedServiceLine.serviceId,
                accountInfoId:this.accountService.memberDetails.accountInfoId
            };

            this.clientService.getClaimCreateInfo(getClaimInfoModel).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
                (x: any) => {
                    this.renderingProviders = x.renderingProviders;
                    this.serviceProviders = x.locations;

                    this.clientReferringProviders = x.referringProviders.filter((f: any) => f.isActive);
                    this.clientReferringProviders.forEach((x) => x.providerName = x.providerName || "Empty Name");
                    this.isClaimInfoNextClicked = true;
                    this.isProviderNextClicked = true;
                    this.checkList();
                    this.billingCodes = x.billingCodes;
                    
                }, (error: any) => {
                    
                });

            this.loadDiagnosisCodes();
        }
    }

    ngOnDestroy() {
        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

    public canDeactivate(): Observable<boolean> | boolean {
        return !this.createClaimForm.dirty || this.isSavingCompleted;
    }

    public cancel(): void {
        if (this.createClaimForm.dirty) {
            this.cancelConfirmationAlert.opened = true;
        }
        else {
            this.backCancelService.emitBackCancel();
        }
    }
}
