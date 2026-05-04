import { AfterViewInit, Directive, ElementRef } from '@angular/core';
declare var $;
@Directive({
  selector: '[appSelect2]'
})
export class Select2Directive implements AfterViewInit {

  constructor(private element: ElementRef) { }
  ngAfterViewInit(){
    this.element.nativeElement.focus();
    $(this.element.nativeElement).select2();
  }
}