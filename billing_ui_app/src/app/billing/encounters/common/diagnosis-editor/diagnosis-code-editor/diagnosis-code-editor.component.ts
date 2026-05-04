import { Component, EventEmitter, forwardRef, Input, Output, ViewChild } from "@angular/core";
import { AuthorizationDiagnosisCode } from "@core/models/clients/authorization";
import { AddDiagnosisPopupComponent } from "../add-diagnosis-popup/add-diagnosis-popup.component";
import { MatDialog } from "@angular/material/dialog";
import { NotificationService } from "@progress/kendo-angular-notification";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";

@Component({
    selector: 'diagnosis-code-editor',
    templateUrl: './diagnosis-code-editor.component.html',
    styleUrls: ['./diagnosis-code-editor.component.css']
})
export class DiagnosisCodeEditorComponent {
    @ViewChild(forwardRef(() => AddDiagnosisPopupComponent)) addDiagnosisPopupComponent: AddDiagnosisPopupComponent;
    @Output() save = new EventEmitter<AuthorizationDiagnosisCode[]>();
    @Output() cancel = new EventEmitter();

    cancelEvent()
    {
        this.cancel.emit();
        document.getElementById("AddField")?.getElementsByTagName("div")[0]?.focus();
    }

    saveEvent(diagnosisCodes:AuthorizationDiagnosisCode[])
    {
        this.save.emit(diagnosisCodes)
        document.getElementById("AddField")?.getElementsByTagName("div")[0]?.focus();
    }

    diagnosisCodes: AuthorizationDiagnosisCode[];

    constructor(private dialog: MatDialog,private notificationService: NotificationHandlerService) {}

    public getOrderName(order: number) {
        switch (order) {
            case 1:
                return "Primary";
            case 2:
                return "Secondary";
            case 3:
                return "Tertiary";
            default:
                return order;
        }
    }


    get getSortedCodes() {
        return this.diagnosisCodes.sort(function (a, b) {
            return a.order - b.order
        });
    }

    get addedCodeIds() {
        return this.diagnosisCodes.map(x => x.diagnosisId);
    }

    loadData(diagnosisCodes: AuthorizationDiagnosisCode[]) {
        this.diagnosisCodes = diagnosisCodes.map(x => Object.assign({}, x));
    }

    downOrder(code: AuthorizationDiagnosisCode) {
        if (code.order >= this.diagnosisCodes.length)
            return;

        let downCode = this.diagnosisCodes.find(x => x.order === code.order + 1);
        if (!downCode)
            return;

        downCode.order--;
        code.order++;


        if (downCode.order === 1)
            downCode.includeOnClaims = true;
    }

    upOrder(code: AuthorizationDiagnosisCode) {
        if (code.order <= 1)
            return;

        let upCode = this.diagnosisCodes.find(x => x.order === code.order - 1);
        if (!upCode)
            return;

        upCode.order++;
        code.order--;

        if (code.order === 1)
            code.includeOnClaims = true;
    }

    removeCode(code: AuthorizationDiagnosisCode) {
        if (code.order === 1)
            return;

        this.diagnosisCodes.remove(code);

        this.diagnosisCodes.where((x: AuthorizationDiagnosisCode) => x.order > code.order).forEach(x => x.order--);
        this.notificationService.showNotificationSuccess("Diagnosis code removed successfully.")
    }

    changeIncludeOnClaims(code: AuthorizationDiagnosisCode)
    {
        code.includeOnClaims=(!code.includeOnClaims)
        this.diagnosisCodes.where((x: AuthorizationDiagnosisCode) => x.order > code.order).forEach(x => x.order--);

    }
    openAddDiagnosis() {
        if (this.diagnosisCodes.length >= 11)
            return;

        //this.addDiagnosisPopupComponent.open();
        this.openAddDiagnosisPopup();
    }
  
    openAddDiagnosisPopup() {
        const dialogRef = this.dialog.open(AddDiagnosisPopupComponent, {
            width: '540px',
            data: { addedCodeIds: this.addedCodeIds}
          });

          dialogRef.afterClosed().subscribe(result => {
            const newDiagnosis: AuthorizationDiagnosisCode = {
                includeOnClaims: true,
                order: this.diagnosisCodes.length + 1,
                description: result.selectedDiagnosisCode.diagnosisLUDescription,
                diagnosisCode: result.selectedDiagnosisCode.diagnosisLUCode,
                diagnosisId: result.selectedDiagnosisCode.diagnosisId,
                manuallyAdded: true,
                isActive: true,
                startDate: new Date(),
                endDate: undefined
            }
            this.diagnosisCodes.push(newDiagnosis);
            this.notificationService.showNotificationSuccess("Diagnosis code added successfully.");
          });
    }
}
