import { Component, ElementRef, Input, OnDestroy, OnInit } from '@angular/core';
import { PrintModalService, PrintModel } from '.';
import { drawDOM, pdf} from '@progress/kendo-drawing';

@Component({
    selector: 'print-modal',
    templateUrl: './print.html',
    styleUrls: ['./print.css']
})
export class PrintComponent implements OnInit, OnDestroy {
   
    @Input() id: string;
    private element: any;
    returnIconName: string | null; 

    constructor(private modalService: PrintModalService, 
        private el: ElementRef) {
        this.element = el.nativeElement;
    }

    ngOnInit(): void {
        let modal = this;

        if (!this.id) {
            console.error('modal must have an id');
            return;
        }

        document.body.appendChild(this.element);

        //close on click outside
        /*this.element.addEventListener('click', function (e: any) {
            if (e.target.className === 'print-modal') {
                modal.close();
            }
        });*/

        this.modalService.add(this);
    }

    ngOnDestroy(): void {
        this.modalService.remove(this.id);
        this.element.remove();
    }

    open(model: PrintModel): void {
        this.returnIconName = model.returnIconName;
        this.element.style.display = 'block';
        document.body.classList.add('print-modal-open');
    }

    close(): void {
        this.returnIconName = null;
        this.element.style.display = 'none';
        document.body.classList.remove('print-modal-open');
        this.modalService.onClose.emit({ closed: true });
    }

    generatePDF(save: boolean) : void {
        const opt = {
            paperSize: 'A4',
            margin: {top : '0cm', bottom: '0cm', left: '0cm', right: '-2cm'},
            landscape: true,
            scale: 0.8
        };

        drawDOM(document.getElementById('print-section'), opt)
        .then(function(group){
            if (save) {
                //Save
                pdf.saveAs(group, "Receipt.pdf");
            }
            else {
                //Print
                pdf.toBlob(group, printToWindow);
            }
        });
    }

    print() {
        this.generatePDF(false);
    }

    savePDF(): void {
        this.generatePDF(true);
    }
}

function printToWindow(data: Blob): void {
    const blobUrl = URL.createObjectURL(data);
    const iframe = document.createElement('iframe');
    document.body.appendChild(iframe);
    iframe.style.display = 'none';
    iframe.src = blobUrl;
    iframe.onload = function () {
            iframe.focus();
            iframe.contentWindow && iframe.contentWindow.print();
    }
}