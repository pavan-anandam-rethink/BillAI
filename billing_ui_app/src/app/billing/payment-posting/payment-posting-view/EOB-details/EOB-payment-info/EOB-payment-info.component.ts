import { Component, Input } from '@angular/core';
import { PaymentEOBInfo } from '@core/models/billing';

@Component({
    selector: 'EOB-payment-info',
    templateUrl: './EOB-payment-info.html',
    styleUrls: ['./EOB-payment-info.css']
})
export class EOBPaymentInfoComponent {
    @Input() payment: PaymentEOBInfo;
    currentDate = new Date();
}