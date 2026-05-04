import { Component, ElementRef, HostBinding, Input, OnInit } from '@angular/core';

import { PaymentInfo } from '@core/models/billing';
import { Locale } from '@app/locale';


@Component({
    selector: 'breakdown-row-template',
    templateUrl: './breakdown-row-template.component.html',
    styleUrls: ['./breakdown-row-template.component.css']
})
export class BreakdownRowTemplateComponent implements OnInit {
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

    constructor(private element: ElementRef, public locale: Locale) { }

    ngOnInit(): void {
        this.height = this.element.nativeElement.parentElement.offsetHeight;
        this.minHeight = Math.max(this.height, 36 * this.rows.length);
    }
}