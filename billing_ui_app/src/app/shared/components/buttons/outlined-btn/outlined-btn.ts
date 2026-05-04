import { Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';

@Component({
    selector: 'outlined-btn',
    template: `
    <div role="button" tabindex="0"  class="outlined-btn small-14bold" [class.isDisabled]="isDisabled" (click)="toggleBtn()" #btn>
        <span class="text">{{text}}</span><span [ngClass]="iconClass" [class.disabled]="isDisabled" class="outlined-icon"></span>
    </div>
   `,
    styleUrls: ['./outlined-btn.css']
})
export class OutlinedBtnComponent {
    @Input() public text = 'outlined-btn';
    @Input() public iconClass = 'outlined-icon';
    @Input() public flip = false;
    @Input() public isDisabled = false;

    @Output() public onClick = new EventEmitter<boolean>();
    active = false;

    @ViewChild('btn', {static: false}) btn: ElementRef;

    constructor() {

    }

    toggleBtn() {
        if(this.isDisabled){
            return;
        }
        if (this.flip) {
            this.active = !this.active;
            const btn = this.btn.nativeElement
            btn && btn.classList.toggle('active');
        }
        this.onClick.emit(this.active);
    }

}
