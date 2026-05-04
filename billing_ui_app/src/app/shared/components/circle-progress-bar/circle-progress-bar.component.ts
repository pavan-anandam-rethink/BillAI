import {Component, Input, AfterViewInit} from '@angular/core';

@Component({
    selector: 'circle-progress-bar',
    template: `
        <div class="d-flex">
            <svg
                    width="20px"
                    height="20px"
                    viewBox="0 0 20 20"
                    preserveAspectRatio="xMidYMid meet"
                    [style.transform]="'rotate('+rotation+'deg)'"
            >
                <circle class="back" r="10" cx="10" cy="10" />
                <circle
                        class="sector"
                        r="5"
                        cx="10"
                        cy="10"
                />
                <circle class="inner" r="7" cx="10" cy="10"/>
            </svg>
        </div>
    `,
    styles: [
        '.inner { fill: var(--Blue-98); }', 
        '.sector { fill: none; stroke: var(--Blue-72); stroke-width: 10; }',
        '.back {fill: var(--Black-88)}']
})
export class CircleProgressBarComponent implements AfterViewInit{
    @Input() value = 0;
    @Input() spinning = false;
    
    rotation = -75;
    
    getStroke(){
        const width = 2 * Math.PI * 5;
        const result = (width / 100) * this.value;
        return result + "," + width;
    }

    ngAfterViewInit(): void {
        if(this.spinning) {
            setInterval(() => {
                this.rotation+=10;
                if(this.rotation > 360)
                    this.rotation -= 360
            }, 16)
        }
    }   
    
}