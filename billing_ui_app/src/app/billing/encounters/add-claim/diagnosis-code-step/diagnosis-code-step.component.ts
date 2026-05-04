import { Component, Input, OnInit, OnDestroy, Output, EventEmitter, SimpleChanges, OnChanges } from "@angular/core";
import { FormGroup, FormBuilder, FormArray, Validators, FormControl } from "@angular/forms";
import { takeUntil } from "rxjs/operators";
import { Subject } from "rxjs";
import { ClientFunderModel, FunderServiceLine } from "@core/models/company-account/funders/client-funder-model";
import { FunderType } from "@core/enums/billing/funder-type";
import { Authorization, AuthorizationDiagnosisCode } from "@core/models/clients/authorization";
import { ClientBillingCode } from "@core/models/billing/billing-code";
import { SidebarService } from "@app/shared/components/sidebar";
import { DiagnosisCodeEditorComponent } from "../../common/diagnosis-editor/diagnosis-code-editor/diagnosis-code-editor.component";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";

@Component({
  selector: 'app-diagnosis-code-step',
  templateUrl: './diagnosis-code-step.component.html',
  styleUrls: ['./diagnosis-code-step.component.css']
})
export class DiagnosisCodeStepComponent implements OnInit, OnChanges, OnDestroy {
  @Input() parentForm: FormGroup;
    @Input() selectedFunder: ClientFunderModel;
    @Input() diagnosisCodes: AuthorizationDiagnosisCode[];
    @Input() billingCodes: ClientBillingCode[];
    @Input() clientId: number;
    @Input() authDetails: Authorization;
    @Input() isProviderNextClicked: boolean;
    @Output() isDiagnosisCodeValid = new EventEmitter<boolean>();
    @Input() selectedServiceLine: FunderServiceLine;
    @Input() authId: number | string;
    diagnosisToSave: AuthorizationDiagnosisCode[];

    public funderIsInsurance = false;
    private unsubscribeAll$ = new Subject<void>();

    stepFormGroup: FormGroup;
    providersFormGroup: FormGroup;
    diagnosisCodesForm: FormGroup;
    diagnosisCodesToSave: FormControl;

    dates: string[] = [];
    filteredDiagnosisCodes: AuthorizationDiagnosisCode[] = [];
    diagnosSequenceOrder = ['Primary', 'Secondary', 'Tertiary'];
    firstSequenceName: string;
    secondSequenceName: string;
    thirdSequenceName: string;

    constructor(private fb: FormBuilder, private sidebarService: SidebarService,
        private notificationHandler: NotificationHandlerService) {
        this.isProviderNextClicked = true;

        this.filteredDiagnosisCodes = [
            {
                description: "Description 1",diagnosisCode: "ABC",diagnosisId: 1, order: 1, includeOnClaims: true,
                manuallyAdded: true,startDate: new Date(),endDate: new Date(), isActive: true
            },
            {
                description: "Description 2",diagnosisCode: "PQR",diagnosisId: 2, order: 2, includeOnClaims: true,
                manuallyAdded: true,startDate: new Date(),endDate: new Date(), isActive: true
            }
        ];
    }

    formCheck(): void {
        if(this.stepFormGroup.get('diagnosisCodes').value.length == 0)
            this.isDiagnosisCodeValid.emit(false);
        else 
            this.isDiagnosisCodeValid.emit(this.stepFormGroup.valid);
    }

    onDiagnosisChange(newDiagnosId: number) {
        const diagnosisForm = this.stepFormGroup.get('diagnosisCodes') as FormGroup;
        const diagnosisFormValues = Object.values(diagnosisForm.value) as number[];
        const origDiagnosisValues = this.filteredDiagnosisCodes.map((x) => x.diagnosisId);

        const newDiagnosCopy = this.getDiagnosisCopy(newDiagnosId);        
        const oldDiagnosisId = origDiagnosisValues.find(id => !diagnosisFormValues.includes(id));
        const oldDiagnosCopy = this.getDiagnosisCopy(oldDiagnosisId);
        this.setDiagnosisOrder(oldDiagnosCopy.diagnosisId, newDiagnosCopy.order);

        const diagnosControlToReplace = diagnosisForm.controls[`diagnosisCode${newDiagnosCopy.order}`];
        diagnosControlToReplace.setValue(oldDiagnosCopy.diagnosisId);
        this.setDiagnosisOrder(newDiagnosCopy.diagnosisId, oldDiagnosCopy.order);
        
        this.diagnosisToSave.sort((a, b) => a.order - b.order);
        this.diagnosisCodesToSave = this.stepFormGroup.get('diagnosisCodesToSave') as FormControl;
        this.diagnosisCodesToSave.setValue(this.diagnosisToSave, { emitEvent: false, onlySelf: true });

        this.setSequenceNames();
    }

    ngOnDestroy(): void {
        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

    ngOnInit() {
        this.initializeForm();
        this.updateValidators();
        this.diagnosisCodesControl = this.stepFormGroup.get("diagnosisCodes") as FormControl;
        this.diagnosisCodesControl.setValue([]);
    }

    initializeForm() {
        this.diagnosisToSave = Array.from(this.filteredDiagnosisCodes);
        this.diagnosisToSave.sort((a, b) => a.order - b.order);
        this.setSequenceNames();
        const mainArray = this.parentForm.get("formData") as FormArray;

        if (mainArray.at(2) == undefined) {
            const formValues = this.fb.group({
                billingCodes: new FormGroup({
                    billingCodes: new FormArray([], Validators.required)
                }),
                diagnosisCodesToSave: this.fb.control([]),
                diagnosisCodes: this.fb.control([]),
            });

            const formArray = this.parentForm.get("formData") as FormArray;
            formArray.push(formValues);
        }

        this.stepFormGroup = mainArray.at(2) as FormGroup;
        this.providersFormGroup = mainArray.at(1) as FormGroup;

        this.parentForm.valueChanges.pipe(takeUntil(this.unsubscribeAll$)).subscribe(() => {
            this.formCheck();
        });
    }

    ngOnChanges(_changes: SimpleChanges): void {
        if (this.isProviderNextClicked) {
            if (this.authId && this.diagnosisCodes) this.diagnosisCodesControl.patchValue(this.diagnosisCodes);
            else this.diagnosisCodesControl.patchValue([]);
            this.initializeForm();
            this.setValidators();
            
        }
    }

    setDiagnosisOrder(id: number, order: number) {
        const diagnos = this.diagnosisToSave.find((f) => f.diagnosisId === id);

        if (diagnos) {
            diagnos.order = order;
        }
    }

    

    diagnosCodeInDOSRange = (minDate: Date, maxDate: Date, code: AuthorizationDiagnosisCode): boolean => {
        const startDate = new Date(code.startDate);        
        const endDate = code.endDate ? new Date(code.endDate) : undefined;

        return (minDate >= startDate && startDate <= maxDate) && (!endDate || endDate && maxDate <= endDate);
    }


    setFormValue() {
        this.filteredDiagnosisCodes.sort((a, b) => a.order - b.order);
        this.stepFormGroup.setControl('diagnosisCodes', new FormGroup({}));
        this.diagnosisCodesForm = this.stepFormGroup.get('diagnosisCodes') as FormGroup;

        for (let i = 0; i < this.filteredDiagnosisCodes.length; i++) {
            const diagnos = this.filteredDiagnosisCodes[i];
            const diagnosisCodeControl = new FormControl(diagnos.diagnosisId);
            const controlName = `diagnosisCode${diagnos.order}`;
            this.diagnosisCodesForm.setControl(controlName, diagnosisCodeControl);
        }

        this.stepFormGroup.patchValue({
            diagnosisCodes: this.diagnosisCodesForm.value,
        });

        this.diagnosisCodesToSave = this.stepFormGroup.get('diagnosisCodesToSave') as FormControl;
        this.diagnosisCodesToSave.setValue(this.filteredDiagnosisCodes);
    }

    setValidators() {
        this.funderIsInsurance = this.selectedFunder.funderType == FunderType.Insurance;        
        const diagnosisCodesToSaveControl = this.stepFormGroup.get('diagnosisCodesToSave') as FormControl;

        diagnosisCodesToSaveControl.setValidators(this.funderIsInsurance ? [Validators.required] : []);
    }

    getDiagnosisCopy(id: number | undefined): AuthorizationDiagnosisCode {
        const diagnos = this.filteredDiagnosisCodes.find((code) => code.diagnosisId == id);

        return Object.assign({}, diagnos);
    }

    setSequenceNames() {
        if (this.diagnosisToSave) {
            const firstDiagnos = this.diagnosisToSave[0];
            this.firstSequenceName = this.diagnosSequenceOrder[firstDiagnos ? firstDiagnos.order - 1 : 0];
            
            const secondDiagnos = this.diagnosisToSave[1];
            this.secondSequenceName = this.diagnosSequenceOrder[secondDiagnos ? secondDiagnos.order - 1 : 1];
            
            const thirdDiagnos = this.diagnosisToSave[2];
            this.thirdSequenceName = this.diagnosSequenceOrder[thirdDiagnos ? thirdDiagnos.order - 1 : 2];
        }
    }
    
    public diagnosisCodesControl: FormControl = new FormControl([]);
    diagnosisCodeClick() {
        if (!this.selectedServiceLine) {
            this.notificationHandler.showNotificationWarning('Please select the Service Line first');
            return;
        }
        this.sidebarService.openRight(DiagnosisCodeEditorComponent, true, "md").subscribe(sidebar => {
            let instance: DiagnosisCodeEditorComponent = sidebar.instance;

            instance.loadData(this.diagnosisCodesControl.value);
            document.getElementById("AddButton")?.getElementsByTagName("div")[0]?.focus();
            instance.save.subscribe((x: AuthorizationDiagnosisCode[]) => {
                this.diagnosisCodesControl.patchValue(x);
                this.stepFormGroup.get('diagnosisCodesToSave').setValue(x);
                this.sidebarService.closeAll();
                document.getElementById("AddService")?.getElementsByTagName("div")[0]?.focus();
            })
            instance.cancel.subscribe((x: any) => {
                this.sidebarService.closeAll();
                document.getElementById("AddService")?.getElementsByTagName("div")[0]?.focus();
            })
        })
    }

    diagnosisCodesString: string = "";

    get getSortedCodes(): AuthorizationDiagnosisCode[] {
        return this.diagnosisCodesControl.value.sort(function (a: AuthorizationDiagnosisCode, b: AuthorizationDiagnosisCode) {
            return a.order - b.order
        });
    }

    get getDiagnosisCodes() {
        return this.getSortedCodes.map(x => x.diagnosisCode).join(", ").toString();
    }

    updateValidators() {
        if (this.funderIsInsurance) {
            this.stepFormGroup.controls['diagnosisCodes'].setValidators([Validators.required]);
        } else {
            this.stepFormGroup.controls['diagnosisCodes'].setValidators([]);
        }

        this.stepFormGroup.controls['diagnosisCodes'].updateValueAndValidity({ emitEvent: false, onlySelf: true });
    }
}