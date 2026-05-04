import { PaymentPostingBreakdownRowTemplateComponent } from './payment-posting-list/breakdown-row-template/payment-posting-breakdown-row-template.component';
import { PaymentNoteDetailsComponent, PaymentNotesComponent } from "./payment-posting-list/payment-notes";
import { PaymentPostingListActionComponent } from './payment-posting-list/payment-posting-list-action/payment-posting-list-action.component';
import { ManualAddPaymentDialogComponent } from './payment-posting-list/payment-posting-list-create-payment-dialog/manual-add-payment-dialog.component';
import { ReceivedCalendarPopupComponent } from './payment-posting-list/payment-posting-list-filter/payment-posting-list-filter-calendar-popup/received-calendar-popup.component';
import { FunderSearchPopupComponent } from './payment-posting-list/payment-posting-list-filter/payment-posting-list-filter-funder-popup/funder-search-popup.component';
import { PaymentPostingListFilterComponent } from './payment-posting-list/payment-posting-list-filter/payment-posting-list-filter.component';
import { PaymentPostingListComponent } from './payment-posting-list/payment-posting-list.component';
import { AttachmentsComponent } from './payment-posting-view/attachments/attachments.component';
import { EOB_DETAILS_COMPONENTS } from "./payment-posting-view/EOB-details";
import { ErrorsComponent } from './payment-posting-view/errors/errors.component';
import { AddPaymentPatientDialogComponent } from './payment-posting-view/manual-posting-patient/add-payment-patient-dialog/add-payment-patient-dialog.component';
import { AddClaimDialogComponent } from './payment-posting-view/add-claim-dialog/add-claim-dialog.component';
import { ManualPaymentDetailsInfoComponent } from './payment-posting-view/manual-posting-patient/manual-payment-details-info/manual-payment-details-info.component';
import { ManualPaymentPatientDetailsComponent } from './payment-posting-view/manual-posting-patient/payment-patient-details/manual-payment-patient-details.component';
import { PaymentClaimPrintFormComponent } from "./payment-posting-view/payment-claim-print-form/payment-claim-print-form.component";
import { PaymentDetailsInfoComponent } from "./payment-posting-view/payment-details-info/payment-details-info.component";
import { ClaimDetailsComponent } from './payment-posting-view/payment-details/claim-details';
import { PatientDetailsComponent } from './payment-posting-view/payment-details/patient-details';
import { PaymentDetailsComponent } from './payment-posting-view/payment-details/payment-details.component';
import { PaymentPostingAdjustmentsComponent } from './payment-posting-view/payment-details/payment-posting-adjustments';
import { PaymentPostingAdjustmentsDetailsComponent } from "./payment-posting-view/payment-details/payment-posting-adjustments-details";
import { PaymentPostingViewComponent } from './payment-posting-view/payment-posting-view.component';
import { InfoEditDialogComponent } from "./payment-posting-view/manual-posting-patient/manual-payment-details-info/info-edit-dialog/info-edit-dialog.component";
import { PaymentDetailsInfoEditDialogComponent } from './payment-posting-view/payment-details-info/payment-details-info-edit-dialog/payment-details-info-edit-dialog.component';
import { PatientClaimDetailsComponent } from './payment-posting-view/manual-posting-patient/patient-claim-details/patient-claim-details.component';
import { ManualUploadEraComponent } from './payment-posting-list/payment-posting-list-create-payment-dialog/manual-upload-era/manual-upload-era.component';
import { EobUploadComponent } from './payment-posting-list/payment-posting-list-create-payment-dialog/eob-upload/eob-upload.component';
import { AddAttachmentDialogComponent } from './payment-posting-view/attachments/add-attachment-dialog/add-attachment-dialog.component';
import { ClaimNotesComponent } from './payment-posting-view/payment-details/claim-notes';
import { PaymentHistoryDetailsComponent } from './payment-posting-view/payment-details/payment-posting-adjustments-details/history-tab/payment-history-details.component';
import { PatientClaimDetailsUnlinkComponent } from './payment-posting-view/manual-posting-patient/patient-claim-details-unlinked/patient-claim-details-unlink.component';
import { BulkPostingComponent } from './bulk-posting/bulk-posting.component';


export {
  PaymentPostingListComponent, PaymentPostingListFilterComponent,
  PaymentPostingBreakdownRowTemplateComponent, PaymentPostingListActionComponent,
  PaymentPostingViewComponent, FunderSearchPopupComponent, ReceivedCalendarPopupComponent,
  ManualAddPaymentDialogComponent, ManualPaymentPatientDetailsComponent,
  AddPaymentPatientDialogComponent, PaymentPostingAdjustmentsDetailsComponent, ClaimNotesComponent,
  PaymentDetailsInfoEditDialogComponent, InfoEditDialogComponent, PaymentHistoryDetailsComponent, BulkPostingComponent
};

export const PAYMENTPOSTING_COMPONENTS = [
  PaymentPostingListComponent,
  PaymentPostingListFilterComponent,
  PaymentPostingBreakdownRowTemplateComponent,
  PaymentPostingListActionComponent,
  FunderSearchPopupComponent,
  ReceivedCalendarPopupComponent,
  ManualAddPaymentDialogComponent,
  PaymentPostingViewComponent,
  PaymentDetailsComponent,
  ...EOB_DETAILS_COMPONENTS,
  PaymentDetailsInfoComponent,
  ErrorsComponent,
  AttachmentsComponent,
  PaymentPostingAdjustmentsComponent,
  ManualPaymentPatientDetailsComponent,
  AddPaymentPatientDialogComponent,
  AddClaimDialogComponent,
  PatientClaimDetailsComponent,
  PatientClaimDetailsUnlinkComponent,
  ManualPaymentDetailsInfoComponent,
  AddAttachmentDialogComponent,
  PaymentClaimPrintFormComponent,
  PaymentDetailsInfoEditDialogComponent,
  ManualUploadEraComponent,
  EobUploadComponent,
  InfoEditDialogComponent,
  BulkPostingComponent
];

/* injectable to modal form components */
export const PAYMENTPOSTING = [
  PaymentPostingAdjustmentsDetailsComponent,
  PaymentHistoryDetailsComponent,
  PaymentNotesComponent,
  PaymentNoteDetailsComponent,
  ClaimDetailsComponent,
  PatientDetailsComponent,
  PaymentPostingViewComponent

];
