import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { AccountMemberService } from "@core/services/account/account-member.service";
import {
    AdjustmentService,
    AppointmentService,
    ChargeEntryService,
    ClaimNotesService,
    ClaimPostingService,
    ClaimService,
    EncounterAttachmentService,
    PaymentNotesService,
    PaymentPostingService
} from "@core/services/billing";
import { ClientsService } from "@core/services/clients/clients.service";
import { KendoModule } from "@app/plugins/kendo.module";
import { MaterialModule } from "@app/plugins/material.module";
import '@app/billing/encounters/common/common.array';
import '@app/billing/encounters/common/common-helper';
import { RouterModule } from "@angular/router";
import { ContainedButtonModule } from "@app/shared/components/buttons/contained-btn/contained-btn.module";
import { OutlinedButtonModule } from "@app/shared/components/buttons/outlined-btn/outlined-btn.module";
import { CurrencyPipe } from "@angular/common";
import { SharedModule } from "@app/shared/components/shared.module";
import { MatDialogRef } from "@angular/material/dialog";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { WriteoffService } from "@core/services/billing/writeoff.service";
import { PatientInvoiceComponent } from "./patient-invoice.component";
import { PatientInvoiceRoutingModule } from "./patient-invoice-routing.module";
import { PatientInvoiceService } from "@core/services/billing/patient-invoice.service";
import { CreateInvoiceComponent } from "./create-invoice/create-invoice.component";
import { CreateInvoiceFiltersComponent } from "./create-invoice/create-invoice-filters/create-invoice-filters.component";
import { CreateInvoiceDetailsComponent } from "./create-invoice/create-invoice-details/create-invoice-details.component";
import { PendingCollectionComponent } from "./pending-collection/pending-collection.component";
import { PendingCollectionDetailsComponent } from "./pending-collection/pending-collection-details/pending-collection-details.component";
import { PatientPopupComponent } from "./shared/patient-popup/patient-popup.component";
import { CalendarPopupComponent } from "./shared/calendar-popup/calendar-popup.component";
import { FilterByRangePopupComponent } from "./shared/filter-by-range-popup/filter-by-range-popup.component";
import { PendingCollectionFiltersComponent } from "./pending-collection/pending-collection-filters/pending-collection-filters.component";
import { PendingCollectionPaymentDialogComponent } from './pending-collection/pending-collection-payment-dialog/pending-collection-payment-dialog.component';

@NgModule({
    declarations: [
        PatientInvoiceComponent,
        CreateInvoiceComponent,
        CreateInvoiceDetailsComponent,
        CreateInvoiceFiltersComponent,
        PatientPopupComponent,
        FilterByRangePopupComponent,
        CalendarPopupComponent,
        PendingCollectionComponent,
        PendingCollectionDetailsComponent,
        PendingCollectionFiltersComponent,
        PendingCollectionPaymentDialogComponent
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
        PatientInvoiceService,
        CurrencyPipe,
        { provide: MatDialogRef, useValue: {} },
    ],
    bootstrap: [],
    imports: [
        CommonModule,
        PatientInvoiceRoutingModule,
        KendoModule,
        MaterialModule,
        FormsModule,
        ReactiveFormsModule,
        RouterModule,
        ContainedButtonModule,
        OutlinedButtonModule,
        SharedModule
    ],
    exports:[CalendarPopupComponent]
})
export class PatientInvoicingModule { }
