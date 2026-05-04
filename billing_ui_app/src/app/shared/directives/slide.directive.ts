import { Directive, ElementRef, HostListener, Input } from '@angular/core';

@Directive({
    selector: '[slide]',
})
export class SlideDirective {
    @Input() slideTarget= '';
    @Input() slideIf = false;
    @Input() slideTo = 0;

    constructor(private elementRef: ElementRef) { }

    @HostListener('click')
    click() {
        const dropdown = document.getElementById(this.slideTarget);
        if (dropdown) {
            dropdown.style.height = this.slideIf ? `${this.slideTo}px` : '0px';
            dropdown.classList.toggle('active');
        }
    }
}