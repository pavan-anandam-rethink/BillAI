import { Component, Input } from '@angular/core';
import { PaymentEOBInfo } from '@core/models/billing';


@Component({
    selector: 'EOB-payee-info',
    templateUrl: './EOB-payee-info.html',
    styleUrls: ['./EOB-payee-info.css']
})
export class EOBPayeeInfoComponent{
    @Input() payment: PaymentEOBInfo;

}