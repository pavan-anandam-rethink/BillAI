import {
  AfterViewInit,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  ViewEncapsulation
} from "@angular/core";
import { PaymentPostingMethods, PaymentSummary } from "@core/models/billing";
import { Observable } from "rxjs";
import { PaymentPostingService } from "@core/services/billing";
import { UpdatePaymentSummary } from "@core/models/billing/update-payment-summary";
// import {toNumbers} from "@angular/compiler-cli/src/diagnostics/typescript_version";
import { filterPaymentMethodByFunderType } from '@app/shared/utils/payment-funder.utils';

@Component({
  selector: 'payment-details-info-edit-dialog',
  templateUrl: './payment-details-info-edit-dialog.html',
  styleUrls: ['./payment-details-info-edit-dialog.css'],
})

export class PaymentDetailsInfoEditDialogComponent implements OnChanges {
  @Input() paymentSummary: PaymentSummary;
  @Output() onSaveChanges = new EventEmitter();
  isOpened: boolean = false;

  editedSummary: PaymentSummary;
  paymentMethods: PaymentPostingMethods[] = [];
  paymentMethodNames: string[] = [];
  paymentMethodValues: string[] = [];

  isUpdated: boolean = false;


  depositDate: Date | undefined;
  postDate: Date | undefined;
  paymentAmount: any;
  filteredPaymentMethods: PaymentPostingMethods[] = [];

  constructor(private paymentPostingService: PaymentPostingService) {
  }

  open(): void {
    this.isOpened = true
    this.depositDate = this.paymentSummary.depositDate ? new Date(this.paymentSummary.depositDate) : undefined;
    this.postDate = this.paymentSummary.postDate ? new Date(this.paymentSummary.postDate) : new Date();
    this.paymentAmount = this.paymentSummary.paymentAmount;
    this.editedSummary = { ...this.paymentSummary };
  }

  close(): void {
    this.isOpened = false;
    this.isUpdated = false;
  }

  save(): void {
    let model: UpdatePaymentSummary = {
      id: this.paymentSummary.id,
      depositDate: this.depositDate,
      postDate: this.postDate,
      paymentMethodId: this.editedSummary.paymentMethodId,
      paymentAmount: this.paymentAmount
    };


    this.paymentPostingService.updatePaymentSummary(model)
      .subscribe(x => {
        this.isOpened = false;
        this.isUpdated = false;
        this.onSaveChanges.emit();
      });
  }

  getPaymentName(id: number) {
    if (id != undefined) {
      let method = this.paymentMethods.find(x => x.enumValue == id.toString());
      return method != undefined ? method.displayName : "";
    }

    return ""
  }

  checkValues() {
    this.isUpdated = true;
  }

  ngOnChanges(changes: SimpleChanges) {
    if (this.paymentSummary !== undefined) {
      this.paymentPostingService.getPaymentMethods()
        .subscribe(x => {
          this.paymentMethods = x;
          // Filter based on paymentTypeId                 
          this.filteredPaymentMethods = this.paymentMethods.filter(method =>
            filterPaymentMethodByFunderType(method.displayName, this.paymentSummary.paymentTypeId, true)
          );

          // Update values for dropdown
          this.paymentMethodNames = this.filteredPaymentMethods.map(y => y.displayName);
          this.paymentMethodValues = this.filteredPaymentMethods.map(y => y.enumValue);
        });
    }
  }

}
