import { Component, ElementRef, HostBinding, Input, OnInit } from '@angular/core';

import { PaymentInfo } from '@core/models/billing';


@Component({
    selector: 'payment-posting-breakdown-row-template',
    templateUrl: './payment-posting-breakdown-row-template.component.html',
    styleUrls: ['./payment-posting-breakdown-row-template.component.css']
})
export class PaymentPostingBreakdownRowTemplateComponent implements OnInit {
    height = 0;
    minHeight = 0;

    @Input() rows: PaymentInfo[] = [];
    @Input() field: string;
    @Input() numberFormat: string;
    @Input() applyColor: boolean;
    @Input() textAlign = 'left';

    @HostBinding('style.height')
    get componentHeight(): string {
        return `${this.height}px`;
    }

    @HostBinding('style.min-height')
    get componentMinHeight(): string {
        return `${this.minHeight}px`;
    }

    constructor(private element: ElementRef) { }

    ngOnInit(): void {
        this.height = this.element.nativeElement.parentElement.offsetHeight;
        this.minHeight = Math.max(this.height, 36 * this.rows.length);
    }
}