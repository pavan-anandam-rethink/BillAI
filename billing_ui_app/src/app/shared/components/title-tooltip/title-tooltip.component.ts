import { Component, Input } from '@angular/core';
import { Align } from '@progress/kendo-angular-popup';

@Component({
    selector: 'title-tooltip',
    templateUrl: './title-tooltip.html',
    styleUrls: ['./title-tooltip.css']
})

export class TitleTooltip {
    @Input() anchor: EventTarget | null;
    @Input() title: string = '';
    @Input() show = false;
    @Input() anchorAlign: Align = { horizontal: 'center', vertical: 'bottom' };
    @Input() popupAlign: Align = { horizontal: 'center', vertical: 'top' };
    @Input() cssClass = '';

    private defaultStyle = 'tooltip-style';
    private followUpStyle = 'follow-up-style';

    private getClass(): string {
        const style: string = this.cssClass === this.followUpStyle ? this.followUpStyle : this.defaultStyle;
        return style;
    }
}