import { AdjustmentService } from './adjustment.service';
import { AppointmentService } from './appointment.service';
import { AttachmentService } from './attachment.service';
import { ChargeEntryService } from './charge-entry.service';
import { ClaimNotesService } from './claim-notes-service';
import { ClaimPostingService } from './claim-posting.service';
import { ClaimService } from './claim.service';
import { EncounterAttachmentService } from './encounter-attachment.service';
import { PaymentNotesService } from './payment-notes-service';
import { PaymentPostingService } from './payment-posting.service';
import { PaymentService } from './payment.service';

export {
    AttachmentService,
    AppointmentService,
    ChargeEntryService,
    ClaimService,
    EncounterAttachmentService,
    PaymentService,
    PaymentPostingService,
    ClaimPostingService,
    AdjustmentService,
    ClaimNotesService,
    PaymentNotesService
};

export const PAYMENTPOSTING_SERVICES = [
    AttachmentService,
    AppointmentService,
    ChargeEntryService,
    ClaimService,
    EncounterAttachmentService,
    PaymentService,
    PaymentPostingService,
    ClaimPostingService,
    AdjustmentService,
    ClaimNotesService,
    PaymentNotesService
];


