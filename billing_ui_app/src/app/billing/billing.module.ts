import { NgModule } from "@angular/core";
import { BillingRoutingModule } from "./billing-routing.module";
//import { ClientHistoryComponent } from './client-history/client-history/client-history.component';
//import { ClientHistoryModule } from "./client-history/client-history.module";
import { KendoModule } from "@app/plugins/kendo.module";

@NgModule({
    declarations: [
    //ClientHistoryComponent
  ],
    providers: [],
    bootstrap: [],
    imports: [
        BillingRoutingModule,
        KendoModule
        ///ClientHistoryModule
    ]
})
export class BillingModule { }
