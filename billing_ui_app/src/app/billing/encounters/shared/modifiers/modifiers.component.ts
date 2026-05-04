import { Component, EventEmitter, Input, OnChanges, Output } from "@angular/core";
import { FormBuilder, FormGroup, Validators } from "@angular/forms";
import { ModifiersHandler } from "./modifiers.handler";
import { ClaimDetailsModel } from "@core/models/billing";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { AccountPermissions } from "@core/enums/account/account-permissions";

export interface ModifiersGridColumnUpdateModel {
    lineId: number;
    modifiers: {
        modifier1: string;
        modifier2: string;
        modifier3: string;
        modifier4: string;
    };
    isValid: boolean;
}

@Component({
    selector: 'modifiers',
    templateUrl: 'modifiers.component.html',
    styleUrls: ['modifiers.component.css'],
})

export class ModifiersComponent implements OnChanges {
    @Input() dataItem: ClaimDetailsModel;
    @Output() onLineUpdate = new EventEmitter();

    formGroup: FormGroup;
    modifiersHandler: ModifiersHandler;
    get m1() { return this.formGroup.controls['modifier1']; }
    get m2() { return this.formGroup.controls['modifier2']; }
    get m3() { return this.formGroup.controls['modifier3']; }
    get m4() { return this.formGroup.controls['modifier4']; }
    canEdit: boolean;

    constructor(private fb: FormBuilder,
        private accountService: AccountMemberService,
        ) {
        this.accountService
        .accountMemberSettings
        .subscribe((x) => {
            if (x) {
                this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEdit);
            }
        });
        this.modifiersHandler = new ModifiersHandler();
        this.formGroup = this.fb.group({
            modifier1: this.fb.control({value:"", disabled: !this.canEdit}, { validators: [Validators.minLength(2)] }),
            modifier2: this.fb.control({value:"", disabled: !this.canEdit}, { validators: [Validators.minLength(2)] }),
            modifier3: this.fb.control({value:"", disabled: !this.canEdit}, { validators: [Validators.minLength(2)] }),
            modifier4: this.fb.control({value:"", disabled: !this.canEdit}, { validators: [Validators.minLength(2)] }),
        }, { validators: [this.modifiersHandler.modifiersValidator] });
        this.modifiersHandler.subscribeModifiers(this.m1, this.m2, this.m3, this.m4, this.updateFormValidity);
    }

    updateFormValidity = () => {
        this.onUpdateModifier();
    }

    ngOnChanges(): void {
        const m1v = this.getValueOrDefault(this.dataItem.modifier1);
        const m2v = this.getValueOrDefault(this.dataItem.modifier2);
        const m3v = this.getValueOrDefault(this.dataItem.modifier3);
        const m4v = this.getValueOrDefault(this.dataItem.modifier4);

        this.formGroup.patchValue({
            modifier1: m1v,
            modifier2: m2v,
            modifier3: m3v,
            modifier4: m4v,
        }, { emitEvent: false });

        if (this.canEdit){
            this.modifiersHandler.validateM1(m1v, this.m1, this.m2, this.m3, this.m4);
            this.modifiersHandler.validateM2(m2v, this.m1, this.m2, this.m3, this.m4);
            this.modifiersHandler.validateM3(m3v, this.m1, this.m2, this.m3, this.m4);
            this.modifiersHandler.validateM4(m4v, this.m1, this.m2, this.m3, this.m4);
        }
    }

    getValueOrDefault(modifier: string) {
        return modifier ? modifier.trim() : "";
    }

    onUpdateModifier(): void {
        const modifiers = this.formGroup.value;
        this.onLineUpdate.emit({
            lineId: this.dataItem.id,
            modifiers: modifiers,
            isValid: this.formGroup.valid
        } as ModifiersGridColumnUpdateModel);
    }
}