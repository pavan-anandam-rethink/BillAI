import { ClaimNoteDetailsComponent, ClaimNotesComponent } from "@app/billing/payment-posting/payment-posting-view/payment-details/claim-notes";
import { LoaderComponent } from "../loader/loader.component";
import { HFCAprintComponent } from "./HFCA/HFCAprint.component";
import { CircleProgressBarComponent } from "./circle-progress-bar/circle-progress-bar.component";
import { NLineTextComponent } from "./n-line-text/n-line-text.component";
import { NotifyComponent } from "./notify-component/notify.component";
import { NotifyDialogComponent } from "./notify-dialog/notify-dialog.component";
import { PrintComponent } from './print-modal';
import { HeaderComponent } from "./header/header.component";
import { ProfileOptionsComponent } from "./header/profile-options/profile-options.component";
import { PrintHCFAPopupComponent } from "./print-hfca-popup/print-hcfa-popup.component";
import { TitleTooltip } from "./title-tooltip/title-tooltip.component";
import { SearchPipe } from "@core/pipe/search.pipe";
import { TruncatePipe } from "../../core/pipe/truncate.pipe";

export {
    CircleProgressBarComponent,
    NLineTextComponent,
    NotifyDialogComponent,
    NotifyComponent,
    PrintComponent,
    HFCAprintComponent,
    HeaderComponent,
    ProfileOptionsComponent
};

export const SHARED_COMPONENTS = [
    CircleProgressBarComponent,
    NLineTextComponent,
    NotifyDialogComponent,
    NotifyComponent,
    PrintComponent,
    HFCAprintComponent,
    LoaderComponent,
    ClaimNotesComponent,
    ClaimNoteDetailsComponent,
    HeaderComponent,
    ProfileOptionsComponent,
    PrintHCFAPopupComponent,
    TitleTooltip,
    SearchPipe,
    TruncatePipe
];