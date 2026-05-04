import { Component, Input } from '@angular/core';
import { PaymentEOBInfo } from '@core/models/billing';

@Component({
    selector: 'EOB-payer-info',
    templateUrl: './EOB-payer-info.html',
    styleUrls: ['./EOB-payer-info.css']
})
export class EOBPayerInfoComponent {
    @Input() payment: PaymentEOBInfo;

}