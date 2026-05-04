import { Directive, ElementRef, Input, OnInit } from '@angular/core';

import { PaymentInfo } from '@core/models/billing';


@Directive({
    selector: '[totalPaidColor]'
})
export class TotalPaidColorDirective implements OnInit {
    @Input('totalPaidColor') paymentInfo: PaymentInfo;
    @Input() applyColor: boolean;

    constructor(private element: ElementRef) { }

    ngOnInit() {
        if (this.applyColor) {
            let color = '';

            const half = this.paymentInfo.totalCharge / 2;
            if (this.paymentInfo.totalPaid === 0) {
                color = 'red';
            } /*else if (this.paymentInfo.totalPaid >= half && this.paymentInfo.totalPaid < this.paymentInfo.totalCharge) {
                color = 'blue';
            } */else if (this.paymentInfo.totalPaid === this.paymentInfo.totalCharge) {
                color = 'green';
            }

            if (color) {
                this.element.nativeElement.style.color = color;
            }
        }
    }
}