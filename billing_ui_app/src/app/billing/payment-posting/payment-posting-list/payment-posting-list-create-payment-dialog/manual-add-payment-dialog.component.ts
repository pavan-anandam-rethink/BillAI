import {
    Component,
    EventEmitter,
    OnDestroy,
    Input,
    Output,
    ViewChild,
} from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { Observable, Subject, forkJoin, of } from "rxjs";
import { PaymentPostingService, ClaimPostingService } from "@core/services/billing";
import { ManualCreatePayment, PaymentPostingListFunderSearch,
    PaymentPostingListFunderSearchBase } from "@core/models/billing";
import { Router } from "@angular/router";
import { map, takeUntil, switchMap } from "rxjs/operators";
import {AutoCompleteComponent, PopupSettings} from "@progress/kendo-angular-dropdowns";
import {ManualUploadEraComponent} from "@app/billing/payment-posting/payment-posting-list/payment-posting-list-create-payment-dialog/manual-upload-era/manual-upload-era.component";
import {EobUploadComponent} from "@app/billing/payment-posting/payment-posting-list/payment-posting-list-create-payment-dialog/eob-upload/eob-upload.component";
import {DatePickerComponent} from "@progress/kendo-angular-dateinputs";
import { AccountMemberService } from '@core/services/account/account-member.service';
import { Helper } from '@app/billing/encounters/common/common-helper';
import { ManualPaymentPatientSearch, ManualPaymentPatientSearchBase, CreatePaymentPatientClaims } from '@core/models/billing';
import { ClientGuarantorInfo } from '@core/models/billing/claim-filter-option-model';
import { PersonaPayService } from '@core/services/billing/personapay.service';
import { PersonaPayWebTokenRequest } from '@core/models/billing/personapay-web-token.request';
import { RevSpringPayloadRequestModel } from '@core/models/billing/revspring-payload.request';
import { RevSpringPayloadResponse } from '@core/models/billing/revspring-payload.response';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { UnallocatedManualCreatePayment } from '@core/models/billing/manual-create-payment';
import { NotificationService } from '@core/services/account/notification.service';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';

@Component({
    selector: 'manual-add-payment-dialog',
    templateUrl: './manual-add-payment-dialog.component.html',
    styleUrls: ['./manual-add-payment-dialog.component.css']
})

export class ManualAddPaymentDialogComponent implements OnDestroy {
    @Output() closeDialogEmitter = new EventEmitter();
    @Output() iframeUrlEmitted = new EventEmitter<SafeResourceUrl>(); 
    // @Output() fileProcessingStarted = new EventEmitter<{fileName: string, uploadId: number}>();
    @Input() isRevSpringEnabled: boolean = false;

    private funderAutocomplete: AutoCompleteComponent;
    isLoading: boolean;
    @ViewChild("funderAutocomplete") set funderAutocomleteRef(funderAutocomleteRef: AutoCompleteComponent) {
        if(funderAutocomleteRef){
            this.funderAutocomplete = funderAutocomleteRef;
        }
    }

    private clientAutocomplete: AutoCompleteComponent;
    @ViewChild("clientAutocomplete")
    set clientAutocompleteRef(clientAutocompleteRef: AutoCompleteComponent) {
        if (clientAutocompleteRef) {
            this.clientAutocomplete = clientAutocompleteRef;
        }
    }   

    @ViewChild(ManualUploadEraComponent) eraUploadComponent: ManualUploadEraComponent;
    @ViewChild(EobUploadComponent) eobUploadComponent: EobUploadComponent;
    
    public webTokenUrl: SafeResourceUrl;
    private unsubscribe: Subject<any> = new Subject();
    
    eobFileId: number | null = null;
    eobFileName: string | null = null;
    pendingEobFile: File | null = null;

    paymentForm: FormGroup = new FormGroup({
        funderType: new FormControl('', [Validators.required]),
        paymentMethod: new FormControl('', [Validators.required]),
        paymentInfo: new FormGroup({})
    });
    
    paymentInfo: FormGroup = new FormGroup({
        depositDate: new FormControl('', [Validators.required]),
        postDate: new FormControl('', [Validators.required]),
        paymentAmount: new FormControl('', [Validators.required]),
        referenceNumber: new FormControl('', [Validators.required]),
    })  
    
    patientForRevspring: FormGroup = new FormGroup({
        paymentAmount: new FormControl('', [Validators.required]),
        referenceNumber: new FormControl(''),
    })
    
    paymentInfoCash: FormGroup = new FormGroup({
        depositDate: new FormControl('', [Validators.required]),
        postDate: new FormControl('', [Validators.required]),
        paymentAmount: new FormControl('', [Validators.required]),
        referenceNumber: new FormControl('', [Validators.required]),
    })    
    
    paymentInfoWithFunder: FormGroup = new FormGroup({
        funderId: new FormControl('', [Validators.required]),
        depositDate: new FormControl('', [Validators.required]),
        postDate: new FormControl('', [Validators.required]),
        paymentAmount: new FormControl('', [Validators.required]),
        referenceNumber: new FormControl('', [Validators.required]),
    })
 
    paymentInfoWithFileId: FormGroup = new FormGroup({
        fileId: new FormControl('', [Validators.required])
    })
    
    funderTypes: string[] = ['Patient', 'Insurance', 'Other'];
    paymentMethodTypesInsurance: string[] = ['Credit Card', 'ACH', 'Check', 'ERA'];
    paymentMethodTypesPatient: string[] = ['Credit Card', 'Cash', 'Check', 'FSA/HSA'];
    paymentMethodTypesOther: string[] = ['Credit Card', 'ACH', 'Check'];
    paymentMethodTypes: string[] = [];
    clientSearchRequest = new ManualPaymentPatientSearchBase();
    clients: ManualPaymentPatientSearch[] = [];
    selectedClients: ManualPaymentPatientSearch[] = [];
    guarantorInfo: ClientGuarantorInfo[] = [];
    selectedClient: ManualPaymentPatientSearch | null = null;
    selectedGuarantor: ClientGuarantorInfo | null = null;
    isGuarantorSetForClaient: boolean = false;

    showFunderDropdown: boolean = false;
    showClientDropdown: boolean = false;
    showAmountsFields: boolean = false;
    showUploadDropzone: boolean = false;
    showEobUpload: boolean = false;

    startedParsingLoading = false;
    
    allowSave: boolean = false;

    isReferenceNumberRequired: boolean = false;


    //because we need to load all funders for user
    funderSearchRequest: PaymentPostingListFunderSearchBase = new PaymentPostingListFunderSearchBase(0);
    funderSearchResult: PaymentPostingListFunderSearch[];
    
    datePickerMinDate = new Date(2000,1,1);
    fileId: number;
    fileName: string;
    
    funderPopupSettings: PopupSettings = {popupClass: 'new-style-popup'};
    clientPopupSettings: PopupSettings = {popupClass: 'new-style-popup'};

    constructor(private fb: FormBuilder, private router: Router, private paymentPostingService: PaymentPostingService, private accountService: AccountMemberService,
         private claimPostingService: ClaimPostingService, private personaPayService: PersonaPayService,  private sanitizer: DomSanitizer, private notifyService: NotificationService, private notificationService: NotificationHandlerService, ) {
        // this.paymentForm = this.fb.group({
        //     funderType: ['', [Validators.required]],
        //     paymentMethod: ['', [Validators.required]],
        //     paymentInfo: this.fb.group({})
        // }, {validators: [this.referenceNumberValidator]});        
    }
    
    get paymentFunderTypeControl(){
        return this.paymentForm.get("funderType")!;
    }
        
    get paymentMethodControl(){
        return this.paymentForm.get("paymentMethod")!;
    }
    
    get paymentInfoControl(){
        return this.paymentForm.get("paymentInfo")!;
    }
    
    onDatePickerClose(element: DatePickerComponent){
         window.setTimeout(() => {element.blur()}, 0);
    }

    funderValueChange(funder: any){
        let selectedFunder = this.funderSearchResult.find(x => x.funderName == funder);

        if(selectedFunder !== undefined) {
            this.paymentInfoControl.patchValue({funderId: selectedFunder.id})
        }
    }

    clientValueChange(client: any){
        this.selectedClient = this.clients.find(x => x.patientName == client);
        this.isGuarantorSetForClaient = false;

        if (this.selectedClient !== undefined) {
            this.paymentInfoControl.patchValue({ clientId: this.selectedClient.id })
            this.getGuarantorDetails(this.selectedClient.id.toString())
                .subscribe(g => {
                    this.selectedGuarantor = g;
                    if (g && g.isGuarantor) {
                        this.isGuarantorSetForClaient = true;
                    }
                    else {
                        this.selectedGuarantor = null;
                        this.notificationService.showNotificationError(
                            'This client does not have Guarantor assigned. Please add one before submitting a Revspring payment.'
                        );
                    }
                });
        }
        else{
            this.clientSearchRequest.personName = "";
            this.searchClients();
            this.selectedGuarantor = null;
        }
    }

    getGuarantorDetails(clientId: string): Observable<ClientGuarantorInfo | null> {
        const cId = Number(clientId);
        return this.paymentPostingService.getGuarantorDetails(cId).pipe(
            map((response: any) => {
                return response ? response : null;
            })
        );
    }
    
    methodChanged(methodValue: any): void {
        if(this.paymentFunderTypeControl.value === "Insurance" && methodValue === "ERA"){
            this.selectedGuarantor = null;
            this.setPaymentInfoWithFileId();
        }

        else if(this.paymentFunderTypeControl.value === "Patient" && methodValue === "RevSpring"){
            this.setPatientForRevspring();
        }
        else  if(methodValue == "Cash"){
            this.selectedGuarantor = null;
            this.setPaymentInfoCash()
        }
        else{
            this.selectedGuarantor = null;
            this.changePaymentInfoControl();
        }      
       
    }

    searchFunders():void {
        this.funderSearchRequest.MemberId = this.accountService.memberDetails.memberId;
        this.funderSearchRequest.AccountInfoId = this.accountService.memberDetails.accountInfoId;
        this.paymentPostingService.getFunders(this.funderSearchRequest)
            .pipe(takeUntil(this.unsubscribe)).subscribe(response => {
                this.funderSearchResult = response.funders;
                this.funderSearchResult.forEach(x => x.id = x.id.toString());
            }
        );
    }

    fundersSearchValueChanged(newVal: string): void {
        window.setTimeout(() => {
            if(newVal == this.funderSearchRequest.funderName){
                this.searchFunders();
            }
        }, 500);

        this.funderSearchRequest.funderName = newVal;
    }

    onFunderSearchClose(event: any): void {
        let activeEl = document.activeElement;

        if(activeEl !== null && activeEl.id == "funder"){
            event.preventDefault();
        }
    }

    searchClients(): void {
        this.isLoading = true;
        this.clientSearchRequest.accountInfoId = this.accountService.memberDetails.accountInfoId;
        this.paymentPostingService.getPatients(this.clientSearchRequest)
        .subscribe(result => {
            this.clients = result.where((x: ManualPaymentPatientSearch) =>
                !this.selectedClients.any((p: ManualPaymentPatientSearch) => p.id === x.id));
            this.clients = this.selectedClients.concat(this.clients);
            this.isLoading = false;
        });
    }

    clientsSearchValueChanged(newVal: string): void {
        window.setTimeout(() => {
            if(newVal == this.clientSearchRequest.personName){
                this.searchClients();
            }
        }, 500);

        this.clientSearchRequest.personName = newVal;
    }

    onClientSearchClose(event: any): void {
        let activeEl = document.activeElement;

        if(activeEl !== null && activeEl.id == "client"){
            event.preventDefault();
        }
        else{
            this.clientSearchRequest.personName = '';
            this.searchClients();
        }
    }

    setEmptyPaymentInfo(){
        if(this.paymentInfoControl == new FormGroup({}))
            return;
        
        this.paymentInfoControl.reset();
        this.paymentForm.setControl("paymentInfo", new FormGroup({}));
        this.showFunderDropdown = false;
        this.showClientDropdown = false;
        this.showAmountsFields = false;
        this.showUploadDropzone = false;
        this.showEobUpload = false;
        this.eobFileId = null;
        this.eobFileName = null;
        this.pendingEobFile = null;
        this.isGuarantorSetForClaient = true;
    }
    
    setPaymentInfo(){     
        if(this.paymentInfoControl == this.paymentInfo)
        return;

        this.paymentInfoControl.reset();
        this.paymentForm.setControl("paymentInfo", this.paymentInfo);
        this.showFunderDropdown = false;
        this.showClientDropdown = false;
        this.showAmountsFields = true;
        this.showUploadDropzone = false;
        this.showEobUpload = false;
        this.eobFileId = null;
        this.eobFileName = null;
        this.pendingEobFile = null;
        this.isReferenceNumberRequired = true;
        this.isGuarantorSetForClaient = true;
    }

    setPaymentInfoWithFunder() {
        // Always update showEobUpload based on current funder type (Insurance vs Other)
        const isInsurance = this.paymentFunderTypeControl.value === 'Insurance';
        this.showEobUpload = isInsurance;
        
        // If EOB upload should not show, clear any uploaded EOB file
        if (!isInsurance) {
            this.eobFileId = null;
            this.eobFileName = null;
            this.pendingEobFile = null;
        }

        if (this.paymentInfoControl == this.paymentInfoWithFunder)
            return;

        this.paymentInfoControl.reset();
        this.paymentForm.setControl("paymentInfo", this.paymentInfoWithFunder);
        this.showFunderDropdown = true;
        this.showClientDropdown = false;
        this.showAmountsFields = true;
        this.showUploadDropzone = false;
        this.eobFileId = null;
        this.eobFileName = null;
        this.pendingEobFile = null;
        this.isReferenceNumberRequired = true;
        this.isGuarantorSetForClaient = true;
    }
       
    setPaymentInfoWithFileId(){
        if(this.paymentInfoControl == this.paymentInfoWithFileId)
            return;

        this.paymentInfoControl.reset();
        this.paymentForm.setControl("paymentInfo", this.paymentInfoWithFileId);
        this.showFunderDropdown = false;
        this.showClientDropdown = false;
        this.showAmountsFields = false;
        this.showUploadDropzone = true;
        this.showEobUpload = false;
        this.eobFileId = null;
        this.eobFileName = null;
        this.pendingEobFile = null;
        this.isGuarantorSetForClaient = true;
    }

    setPatientForRevspring(){
        if(this.paymentInfoControl == this.patientForRevspring)
            return;

        this.paymentInfoControl.reset();
        this.paymentForm.setControl("paymentInfo", this.patientForRevspring);
        this.showFunderDropdown = false;
        this.showClientDropdown = true;
        this.showAmountsFields = true;
        this.showUploadDropzone = false;
        this.showEobUpload = false;
        this.eobFileId = null;
        this.eobFileName = null;
        this.pendingEobFile = null;
        this.isReferenceNumberRequired = true;
        this.startedParsingLoading = false;
    }
           
    setPaymentInfoCash(){
        if(this.paymentInfoControl == this.paymentInfoCash)
            return;

        this.paymentInfoControl.reset();
        this.paymentForm.setControl("paymentInfo", this.paymentInfoCash);
        this.showFunderDropdown = false;
        this.showClientDropdown = false;
        this.showAmountsFields = true;
        this.showUploadDropzone = false;
        this.showEobUpload = false;
        this.eobFileId = null;
        this.eobFileName = null;
        this.pendingEobFile = null;
        this.isReferenceNumberRequired = false;
        this.isGuarantorSetForClaient = true;
    }
    
    changePaymentInfoControl(){
        switch (this.paymentFunderTypeControl.value) {
            case 'Patient':
                this.setPaymentInfo();
                this.paymentMethodTypes = this.paymentMethodTypesPatient;
                if (this.isRevSpringEnabled && !this.paymentMethodTypes.includes('RevSpring')) {
                    this.paymentMethodTypes.push('RevSpring');
                }
                break;
            case 'Insurance':
                this.setPaymentInfoWithFunder();
                this.paymentMethodTypes = this.paymentMethodTypesInsurance;
                break;
            case 'Other':
                this.setPaymentInfoWithFunder();
                this.paymentMethodTypes = this.paymentMethodTypesOther;
                break;
            default:
                this.setEmptyPaymentInfo();
                this.paymentMethodTypes = [];
                break;
        }
    }
    
    funderTypeChange(value: string): void {
        this.paymentMethodControl.reset();        
        this.changePaymentInfoControl();
    }

    close() {
        this.closeDialogEmitter.emit();
        
        if(this.eraUploadComponent) {
            this.eraUploadComponent.deleteAttachments();
        }
        if(this.eobUploadComponent) {
            this.eobUploadComponent.deleteAttachments();
        }
    }

    createPayment(): void {
        if (!this.paymentForm.valid) {
            return;
        }
        this.isLoading = true;
        let formValue = this.paymentForm.value;

        // ERA (Insurance) flow remains unchanged
        if (!(formValue.funderType == 'Insurance' && formValue.paymentMethod == 'ERA')) {
            // RevSpring flow: do NOT pre-create Payment Posting record
            if (formValue.funderType === 'Patient' && formValue.paymentMethod === 'RevSpring') {
                // Ensure guarantor details are present
                if (!this.selectedGuarantor || !this.isGuarantorSetForClaient) {
                    this.isLoading = false;
                    this.notificationService.showNotificationError(
                        'This client does not have Guarantor assigned. Please add one before submitting a RevSpring payment.'
                    );
                    return;
                }
                const rawAmount = formValue.paymentInfo.paymentAmount;
                const formattedAmount = rawAmount.toFixed(2); 

                // First fetch RevSpring payload from backend, then include it in the PersonaPay request
                const payloadRequest: RevSpringPayloadRequestModel = {
                    AccountInfoId: this.accountService.memberDetails.accountInfoId,
                    MemberId: this.accountService.memberDetails.memberId,
                    ClientId: this.selectedGuarantor.userId,
                    AmountDue: formattedAmount,
                    UserEmail: this.selectedGuarantor.email,
                    UserLastName: this.selectedGuarantor?.name?.lastName,
                    ReferenceNo: formValue.paymentInfo.referenceNumber
                };

                this.paymentPostingService.getRevSpringPayload(payloadRequest).subscribe({
                    next: (rsPayload: RevSpringPayloadResponse) => {
                        if (rsPayload?.payload?.dataContext?.consumer) {
                            rsPayload.payload.dataContext.consumer.amountDue = formattedAmount;
                        }

                        const personaRequest: PersonaPayWebTokenRequest = {
                            payload: rsPayload?.payload
                        };


                        this.personaPayService.createWebToken(personaRequest).subscribe({
                            next: (response: any) => {
                                 if (response?.success===true) {
                                this.closeDialogEmitter.emit();
                                const webTokenLink = response.webTokenLink;
                                this.webTokenUrl = this.sanitizer.bypassSecurityTrustResourceUrl(webTokenLink);
                                this.iframeUrlEmitted.emit(this.webTokenUrl);
                                 }
                                else {
                                    console.log('RevSpring returned failure response', response?.message);
                                    this.notificationService.showNotificationError(
                                        response?.message || 'Failed to initiate RevSpring payment.'
                                    );
                                }
                                this.isLoading = false;
                            },
                            error: (error) => {
                                this.isLoading = false;
                                console.log('error while initiating RevSpring payment', error);
                                this.notificationService.showNotificationError(
                                    error?.error?.message || 'Failed to initiate RevSpring payment.'
                                );
                            }
                        });
                    },
                    error: (error) => {
                        this.isLoading = false;
                        console.log('error while retrieving RevSpring payload', error);
                        this.notificationService.showNotificationError('Failed to retrieve RevSpring payload.');
                    }
                });

                return; // Do not continue to Payment Posting creation
            }

            // Non-RevSpring methods: preserve existing behavior (create Payment Posting record)
            let patientModel: UnallocatedManualCreatePayment = {
                funderType: formValue.funderType,
                paymentMethod: formValue.paymentMethod,
                paymentAmount: formValue.paymentInfo.paymentAmount,
                referenceNumber: formValue.paymentInfo.referenceNumber,
                postDate: Helper.shiftDateToUTC(new Date(formValue.paymentInfo.postDate)),
                depositDate: Helper.shiftDateToUTC(new Date(formValue.paymentInfo.depositDate)),
                AccountInfoId: this.accountService.memberDetails.accountInfoId,
                MemberId: this.accountService.memberDetails.memberId,
                patientId: null,
                unAllocatedAmount: null,
                notes: '',
            }

            if (formValue.funderType != "Patient") {
                patientModel.funderId = formValue.paymentInfo.funderId;
            }

            // Create payment first, then upload EOB file if pending
            this.paymentPostingService.manualCreatePatientPayment(patientModel)
                .pipe(
                    switchMap((paymentId: number) => {
                        // If EOB file is pending for Insurance payments, upload it after payment creation
                        if (formValue.funderType === 'Insurance' && this.pendingEobFile && this.eobUploadComponent) {
                            return this.eobUploadComponent.uploadFile(paymentId).pipe(
                                map((uploadResult: { fileName: string, fileId: number } | null) => {
                                    if (uploadResult) {
                                        this.eobFileId = uploadResult.fileId;
                                    }
                                    return paymentId;
                                })
                            );
                        }
                        return of(paymentId);
                    })
                )
                .subscribe((result: number) => {
                    this.router.navigateByUrl('/billing/paymentposting/edit/' + result);
                }, error => {
                    this.isLoading = false;
                    console.log('error while creating payment', error);
                });
        } else {
            this.startedParsingLoading = true;
            this.paymentPostingService.startPaymentParsing(this.fileId)
                .subscribe(x => {
                    this.startedParsingLoading = false;
                    this.closeDialogEmitter.emit();
                    this.isLoading = false;
                    // this.fileProcessingStarted.emit({fileName: this.fileName, uploadId: this.fileId});
                });
        }
    }
   
    showFunderAutocomleteList(): void {
        this.searchFunders();
        
        this.funderAutocomplete.toggle(true);

        const width = this.funderAutocomplete.width;
        this.forcePopupWidth(width.max);
    }
    
    showClientAutocomleteList(): void {
        this.searchClients();
        
        this.clientAutocomplete.toggle(true);

        const width = this.clientAutocomplete.width;
        this.forcePopupWidth(width.max);
    }

    forcePopupWidth(width: string){
        window.setTimeout(() => {
            const popupDiv: any = document.getElementsByClassName("k-popup k-list-container k-reset new-style-popup")[0];
            popupDiv.style.width = width;
        }, 100);
    }
       
    fileUploadedEvent(fileName: string, fileId: number): void {
        this.fileId = fileId;
        this.fileName = fileName;
        this.paymentInfoControl.patchValue({fileId: fileId})
        
    }

    eobFileSelectedEvent(event: { fileName: string, file: File } | null): void {
        if (event) {
            this.pendingEobFile = event.file;
            this.eobFileName = event.fileName;
        } else {
            this.pendingEobFile = null;
            this.eobFileName = null;
            this.eobFileId = null;
        }
    }

    referenceNumberValidator(fg: FormGroup): ValidationErrors | null  {
        let paymentMethod = fg.get("paymentMethod") as FormControl;
        let nonEraData = fg.get("funderNonEraMethodInfo")!.get("0") as FormGroup;
        
        if(nonEraData == null){
            return null;
        }
        
        let referenceNumber = nonEraData.get("referenceNumber") as FormControl;
        
        if(paymentMethod.value != "Cash" && referenceNumber.value.trim() == ""){
            return referenceNumber;
        }else{
            return null;
        }
    }

    ngOnDestroy() {
        this.unsubscribe.next(void 0);
        this.unsubscribe.complete();
    }
}
