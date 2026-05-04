import { Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';

@Component({
    selector: 'contained-btn',
    template: `
    <div role="button" tabindex="0" class="contained-btn small-14bold" [class.isDisabled]="isDisabled" (click)="toggleBtn()" #btn [style.width]="width">
        <span class="text">{{text}}</span><span [ngClass]="iconClass" [class.disabled]="isDisabled" class="contained-icon"></span>
    </div>
   `,
    styleUrls: ['./contained-btn.css']
})
export class ContainedBtnComponent {
    @Input() public text = 'contained-btn';
    @Input() public iconClass = 'contained-icon';
    @Input() public flip = false;
    @Input() public isDisabled = false;
    @Input() public width = "auto"

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
