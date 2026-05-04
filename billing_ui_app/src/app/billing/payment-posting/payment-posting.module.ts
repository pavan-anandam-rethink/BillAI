import '@app/billing/encounters/common/common.array';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { RouterModule } from '@angular/router';
import { AdjustmentService, ClaimPostingService, PaymentPostingService, PAYMENTPOSTING_SERVICES } from '@app/core/services/billing';
import { KendoModule } from '@app/plugins/kendo.module';
import { HttpService } from '@core/services/http.service';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { HeaderModule } from "@progress/kendo-angular-grid";
import { InputsModule } from '@progress/kendo-angular-inputs';
import { PopupModule } from "@progress/kendo-angular-popup";
import { PAYMENTPOSTING_COMPONENTS, PAYMENTPOSTING } from '.';
import { ClaimService } from '@core/services/billing';
import { SharedModule } from '@app/shared/components/shared.module';
import { ContainedButtonModule } from '@app/shared/components/buttons/contained-btn/contained-btn.module';
import { OutlinedButtonModule } from '@app/shared/components/buttons/outlined-btn/outlined-btn.module';
import { MaterialModule } from '@app/plugins/material.module';
import { AttachmentService } from '@core/services/billing/attachment.service';
import { PaymentPostingRoutingModule } from './payment-posting-routing.module';
import { WriteoffService } from '@core/services/billing/writeoff.service';
import { PaymentPostingListFilterStatusPopupComponent } from './payment-posting-list/payment-posting-list-filter/payment-posting-list-filter-status-popup/payment-posting-list-filter-status-popup.component';
import { PaymentPostingListFilterMethodPopupComponent } from './payment-posting-list/payment-posting-list-filter/payment-posting-list-filter-method-popup/payment-posting-list-filter-method-popup.component';
import { PaymentPostingListFilterDateReceivedPopupComponent } from './payment-posting-list/payment-posting-list-filter/payment-posting-list-filter-date-received-popup/payment-posting-list-filter-date-received-popup.component';
import { PaymentPostingListFilterModePopupComponent } from './payment-posting-list/payment-posting-list-filter/payment-posting-list-filter-mode-popup/payment-posting-list-filter-mode-popup.component';
import { ClaimsManagementFilterService } from '../services/claims-management-filter.service';
import { BulkPostingComponent } from './bulk-posting/bulk-posting.component';
import { ClaimFollowupNotesComponent } from './payment-posting-view/payment-details/claim-followup-notes/claim-followup-notes.component';
import { PaymentDetailsFilterComponent } from './payment-posting-view/payment-details/payment-details-filter/payment-details-filter.component';
import { ClientFilterPopupComponent } from './payment-posting-view/payment-details/payment-details-filter/client-filter-popup/client-filter-popup/client-filter-popup.component';
import { EditUnallocatedDialogComponent } from './payment-posting-view/manual-posting-patient/unallocated-payment-posting-view/edit-unallocated-dialog.component';

@NgModule({
  imports: [
    FormsModule,
    MatTabsModule,
    InputsModule,
    ReactiveFormsModule,
    KendoModule,
    NgbModule,
    ContainedButtonModule,
    OutlinedButtonModule,
    RouterModule,
    PaymentPostingRoutingModule,
    SharedModule,
    PopupModule,
    HeaderModule,
    MaterialModule
  ],
  declarations: [
    PAYMENTPOSTING_COMPONENTS,
    PAYMENTPOSTING,
    PaymentPostingListFilterStatusPopupComponent,
    PaymentPostingListFilterMethodPopupComponent,
    PaymentPostingListFilterDateReceivedPopupComponent,
    PaymentPostingListFilterModePopupComponent,
    BulkPostingComponent,
    ClaimFollowupNotesComponent,
    PaymentDetailsFilterComponent,
    ClientFilterPopupComponent,
    EditUnallocatedDialogComponent
  ],
  providers: [
    // HttpService,
    ClaimPostingService,
    AdjustmentService,
    ClaimService,
    PaymentPostingService,
    PAYMENTPOSTING_SERVICES,
    AttachmentService,
    WriteoffService,
    BulkPostingComponent
  ]
})
export class PaymentPostingModule {
  constructor(myService: ClaimsManagementFilterService) {
    myService.isFilterSet = false;
  }
}
