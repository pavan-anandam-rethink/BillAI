import { NgModule } from "@angular/core";
import { ModifiersComponent } from "@app/billing/encounters/shared/modifiers/modifiers.component";
import { EncounterViewComponent } from "./encounter-view/encounter-view.component";
import { EncounterDetailsComponent } from "./encounter-view/encounter-details/encounter-details.component";
import { EncounterDetailsClientInfoComponent } from "./encounter-view/encounter-details/encounter-details-client-info/encounter-details-client-info.component";
import { EncounterDetailsChargeDetailSummaryComponent } from "./encounter-view/encounter-details/encounter-details-charge-detail-summary/encounter-details-charge-detail-summary.component";
import { EncounterDetailsProvidersComponent } from "./encounter-view/encounter-details/encounter-details-providers/encounter-details-providers.component";
import { EncounterDetailsAdditionalInfoComponent } from "./encounter-view/encounter-details/encounter-details-additional-info/encounter-details-additional-info.component";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { AdjustmentService, 
    AppointmentService,
    ChargeEntryService, 
    ClaimNotesService, 
    ClaimPostingService, 
    ClaimService, 
    EncounterAttachmentService, 
    PaymentNotesService, 
    PaymentPostingService } from "@core/services/billing";
import { ClientsService } from "@core/services/clients/clients.service";
import { AddClaimComponent } from "./add-claim/add-claim.component";
import { ClaimInfoStepComponent } from "./add-claim/claim-info-step/claim-info-step.component";
import { DiagnosisCodeStepComponent } from "./add-claim/diagnosis-code-step/diagnosis-code-step.component";
import { ProviderStepComponent } from "./add-claim/provider-step/provider-step.component";
import { BillingCodesComponent } from "./add-claim/diagnosis-code-step/billing-codes/billing-codes.component";
import { BillingCodeAddNoteComponent } from "./add-claim/diagnosis-code-step/billing-codes/note/billing-code-note.component";
import { NoAuthDialogComponent } from "./common/no-auth-dialog/no-auth-dialog.component";
import { EncoutersRoutingModule } from "./encounters-routing.module";
import { KendoModule } from "@app/plugins/kendo.module";
import { MaterialModule } from "@app/plugins/material.module";
import { RouterModule } from "@angular/router";
import { ContainedButtonModule } from "@app/shared/components/buttons/contained-btn/contained-btn.module";
import { OutlinedButtonModule } from "@app/shared/components/buttons/outlined-btn/outlined-btn.module";
import { ChargeNotesComponent } from './encounter-view/encounter-details/encounter-details-charge-detail-summary/charge-notes/charge-notes.component';
import { CurrencyPipe } from "@angular/common";
import './common/common-helper';
import './common/common.array';
import { EncounterTransactionComponent } from "./encounter-view/encounter-transaction/encounter-transaction.component";
import { ClaimHistoryListFilterComponent } from "./encounter-view/claim-history-list-sort-filter/claim-history-list-filter.component";
import { SearchPopupComponent } from "./encounter-view/claim-history-list-sort-filter/claim-history-list-filter-search-popup/search-popup.component";
import { CalendarPopupComponent } from "./encounter-view/claim-history-list-sort-filter/claim-history-list-filter-calendar-popup/calendar-popup.component";
import { AddDiagnosisPopupComponent } from "./common/diagnosis-editor/add-diagnosis-popup/add-diagnosis-popup.component";
import { DiagnosisCodeEditorComponent } from "./common/diagnosis-editor/diagnosis-code-editor/diagnosis-code-editor.component";
import { EncounterAttachmentsComponent } from './encounter-view/encounter-attachments/encounter-attachments.component';
import { ConfirmDialogModule } from "@app/shared/components/confirmation-dialog/confirm-dialog.module";
import { EncounterErrorsAlertsComponent } from './encounter-view/encounter-errors-alerts/encounter-errors-alerts.component';
import { EncounterErrorsAlertsDetailsComponent } from './encounter-view/encounter-errors-alerts/encounter-errors-alerts-details/encounter-errors-alerts-details.component';
import { SharedModule } from "@app/shared/components/shared.module";
import { AddClaimAttachmentDialogComponent } from "./encounter-view/encounter-attachments/add-claim-attachment-dialog/add-claim-attachment-dialog/add-claim-attachment-dialog.component";
import { EncounterAppointmentsComponent } from "./encounter-view/encounter-appointments/encounter-appointments.component";
import { AppointmentAddComponent } from "./encounter-view/encounter-appointments/appointment-add/appointment-add.component";
import { MatDialogRef } from "@angular/material/dialog";
import { ClaimFiltersStatusPopupComponent } from "./ecnounter-list/claim-filters/claim-filters-status-popup/claim-filters-status-popup.component";
import { ClaimFiltersValidationPopupComponent } from "./ecnounter-list/claim-filters/claim-filters-validation-popup/claim-filters-validation-popup.component";
import { RebillClaimDialogComponent } from "./ecnounter-list/rebill-claim-dialog/rebill-claim-dialog.component";
import { VoidClaimDialogComponent } from "./ecnounter-list/void-claim-dialog/void-claim-dialog.component";
import { BillNextFunderDialogComponent } from "./ecnounter-list/bill-next-funder-dialog/bill-next-funder-dialog.component";
import { WriteOffClaimDialogComponent } from "./ecnounter-list/write-off-claim-dialog/write-off-claim-dialog.component";
import { AddClaimNotesDialogComponent } from "./ecnounter-list/add-claim-notes-dialog/add-claim-notes-dialog.component";
import { EncounterListActionComponent } from "./ecnounter-list/encounter-list-action/encounter-list-action.component";
import { SubmitReasonDialogComponent } from "./ecnounter-list/submit-reason-dialog/submit-reason-dialog.component";
import { BillingCodeModsComponent } from "./ecnounter-list/encounter-list-details/billing-code/mods-adjustments-tab/billing-code-mods.component";
import { BillingCodeHistoryComponent } from "./ecnounter-list/encounter-list-details/billing-code/history-tab/billing-code-history.component";
import { BillingCodeDetailsComponent } from "./ecnounter-list/encounter-list-details/billing-code/details-tab/billing-code-details.component";
import { BillingCodeComponent } from "./ecnounter-list/encounter-list-details/billing-code/billing-code.component";
import { EncounterListDetailsComponent } from "./ecnounter-list/encounter-list-details/encounter-list-details.component";
import { SelectorPopupComponent } from "./ecnounter-list/selector-popup/selector-popup.component";
import { EncounterListComponent } from "./ecnounter-list/encounter-list.component";
import { ClaimFiltersComponent } from "./ecnounter-list/claim-filters/claim-filters.component";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { ClaimFiltersPatientPopupComponent } from "./ecnounter-list/claim-filters/claim-filters-patient-popup/claim-filters-patient-popup.component";
import { ClaimFiltersReasonCodePopupComponent } from "./ecnounter-list/claim-filters/claim-filters-reasoncode-popup/claim-filters-reasoncode-popup.component";
import { ClaimFiltersFunderPopupComponent } from "./ecnounter-list/claim-filters/claim-filters-funder-popup/claim-filters-funder-popup.component";
import { ClaimFiltersLocationPopupComponent } from "./ecnounter-list/claim-filters/claim-filters-location-popup/claim-filters-location-popup.component";
import { ClaimFiltersRenderingProviderPopupComponent } from "./ecnounter-list/claim-filters/claim-filters-rendering-provider-popup/claim-filters-rendering-provider-popup.component";
import { EncounterResponseDetailsComponent } from './encounter-view/encounter-errors-alerts/encounter-response-details/encounter-response-details.component';
import { WriteoffService } from "@core/services/billing/writeoff.service";
import { ClaimFiltersCalendarPopupComponent } from "./ecnounter-list/claim-filters/claim-filters-calendar-popup/claim-filters-calendar-popup.component";
import { FlagClaimDialogComponent } from "./ecnounter-list/flag-claim-dialog/flag-claim-dialog.component";
import { ClaimFiltersFlaggedreasonPopupComponent } from './ecnounter-list/claim-filters/claim-filters-flaggedreason-popup/claim-filters-flaggedreason-popup.component';
import { ClaimFiltersAssigneePopupComponent } from "./ecnounter-list/claim-filters/claim-filters-assignee-popup/claim-filters-assignee-popup.component";

@NgModule({
    declarations: [
        AddClaimComponent,
        ClaimInfoStepComponent,
        DiagnosisCodeStepComponent,
        ProviderStepComponent,
        EncounterListComponent,
        BillingCodesComponent,
        BillingCodeAddNoteComponent,
        ModifiersComponent,
        DiagnosisCodeEditorComponent,
        AddDiagnosisPopupComponent,
        NoAuthDialogComponent,
        EncounterViewComponent,
        EncounterDetailsComponent,
        EncounterDetailsClientInfoComponent,
        EncounterDetailsChargeDetailSummaryComponent,
        EncounterDetailsProvidersComponent,
        EncounterDetailsAdditionalInfoComponent,
        EncounterAppointmentsComponent,
        AppointmentAddComponent,
        ChargeNotesComponent,
        ModifiersComponent,
        EncounterTransactionComponent,
        ClaimHistoryListFilterComponent,
        SearchPopupComponent,
        CalendarPopupComponent,
        EncounterAttachmentsComponent,
        EncounterErrorsAlertsComponent,
        EncounterErrorsAlertsDetailsComponent,
        AddClaimAttachmentDialogComponent,
        VoidClaimDialogComponent,
        RebillClaimDialogComponent,
        BillNextFunderDialogComponent,
        WriteOffClaimDialogComponent,
        AddClaimNotesDialogComponent,
        ClaimFiltersValidationPopupComponent,
        ClaimFiltersStatusPopupComponent,
        EncounterListActionComponent,
        SelectorPopupComponent,
        EncounterListDetailsComponent,
        BillingCodeComponent,
        BillingCodeDetailsComponent,
        BillingCodeHistoryComponent,
        BillingCodeModsComponent,
        SubmitReasonDialogComponent,
        ClaimFiltersComponent,
        ClaimFiltersPatientPopupComponent,
        ClaimFiltersFunderPopupComponent,
        ClaimFiltersLocationPopupComponent,
        ClaimFiltersReasonCodePopupComponent,
        ClaimFiltersRenderingProviderPopupComponent,
        EncounterResponseDetailsComponent,
        ClaimFiltersCalendarPopupComponent,
        FlagClaimDialogComponent,
        ClaimFiltersFlaggedreasonPopupComponent,
        ClaimFiltersAssigneePopupComponent
    ],
    providers: [
        AccountMemberService,
        ChargeEntryService,
        AppointmentService,
        EncounterAttachmentService,
        PaymentPostingService,
        AdjustmentService,
        WriteoffService,
        ClaimNotesService,
        PaymentNotesService,
        ClaimPostingService,
        ClientsService,
        ClaimService,
        CurrencyPipe,
        { provide: MatDialogRef, useValue: {} },
    ],
    bootstrap: [],
    imports: [
        EncoutersRoutingModule,
        KendoModule,
        MaterialModule,
        FormsModule,
        ReactiveFormsModule,
        RouterModule,
        ContainedButtonModule,
        OutlinedButtonModule,
        ConfirmDialogModule,
        SharedModule
    ]
})
export class EncoutersModule { }
