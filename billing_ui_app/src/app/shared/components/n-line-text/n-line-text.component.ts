import { AfterViewInit, Component, ElementRef, Input, ViewChild } from '@angular/core';

@Component({
    selector: 'nline-text',
    template: `
            <div class="body" [ngClass]="textClass">
                <div #val class="value" [ngClass]="{clipped: clipped}"
                    [style.-webkit-line-clamp]="nlines"
            >
                    {{text}}
                </div>
            </div>
            <div *ngIf="haveMore" class="small-12reg more" (click)="clipped = !clipped">
                {{clipped ? 'See more' : 'See less'}}
            </div>
    `,
    styles: [`
        .body .value {
             word-wrap: break-word;
        }
        .body .value.clipped {
            overflow: hidden;
            text-overflow: ellipsis;
            display: -webkit-box;
            -webkit-box-orient: vertical;
        }
        .more {
            color: var(--Blue-72);
            cursor: pointer;
        }
    `]
})
export class NLineTextComponent implements AfterViewInit {


    @Input() public text = 'some text';
    @Input() public nlines = 3;
    @Input() public textClass: string;
    clipped = true;
    haveMore = false;

    @ViewChild('val') value: ElementRef;

    ngAfterViewInit() {
        setTimeout(() => {
            if (this.text.length > 150) {
                this.haveMore = true;
            }
        });
    }
}