import {Component, EventEmitter, Output} from "@angular/core";
import {ClaimDetailsModel} from "@core/models/billing";

@Component({
    selector: 'billing-code',
    templateUrl: './billing-code.component.html',
    styleUrls: ['./billing-code.component.css',]
})
export class BillingCodeComponent {
    @Output() updateLineEmitter = new EventEmitter();
    
    chargeId: number;
    claimId: number;
    modelData: ClaimDetailsModel;
    
    setData(chargeId: number, charge: ClaimDetailsModel, claimId: number) {
        this.chargeId = chargeId;
        this.modelData = charge;
        this.modelData.expectedAmount = this.modelData.billedAmount;
        this.claimId = claimId
    }

    updateLines()
    {
        this.updateLineEmitter.emit();
    }
}