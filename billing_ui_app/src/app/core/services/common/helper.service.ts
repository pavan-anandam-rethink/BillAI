import { Injectable } from '@angular/core';
import { IntlService } from '@progress/kendo-angular-intl';
import { saveAs } from 'file-saver';

@Injectable({
    providedIn: 'root'
})
export class HelperService {
    constructor(private intl: IntlService) { }

    download(blob: any, fileName: string): void {
        var extension = 'csv';

        if (blob.type === 'text/csv') {
            extension = 'csv';
        }

        if (blob.type === 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet') {
            extension = 'xlsx';
        }

        if (blob.type === 'application/pdf') {
            extension = 'pdf';
        }

        if (blob.type === 'image/png') {
            extension = 'png';
        }

        saveAs(blob, fileName + '.' + extension);
    }

    getHealthcareTabUrl(tab: HelperService.HealthcareTab, childProfileId: number | null): string {
        let url = tab == HelperService.HealthcareTab.FileCabinet ? `clients/${childProfileId || 0}/files` : `clients/${childProfileId||0}`;
        return url;
    }

    parseDate(json: any) {
        Object.keys(json).map(key => {
            const date = this.intl.parseDate(json[key]);
            if (date !== null) {
                json[key] = date;
            }
        });

        return json;
    }

    getBase64Image(img: HTMLImageElement) {
        img.setAttribute('crossOrigin', '');
        const canvas = document.createElement("canvas");
        canvas.width = img.width;
        canvas.height = img.height;
        const ctx = canvas.getContext("2d");
        ctx && ctx.drawImage(img, 0, 0, img.width, img.height);
        const dataURL = canvas.toDataURL("image/jpg");
        return dataURL;
    }
    

    viewImageInNewWindow( data : { url: string, blob?: Blob, path?: string}) {
        if (data.blob) {
            let reader = new FileReader();
            reader.readAsDataURL(data.blob);
            reader.onloadend = () => {
                let base64data = reader.result;
                let image = new Image();
                image.src = base64data as string;
                this.openWindow(image, data.url);
            }
        } else if (data.path) {
            let image = new Image();
            image.src = data.path;
            this.openWindow(image, data.url);
        }
    }

    private openWindow(image: HTMLImageElement, url: string): void {
        image.style.position = 'absolute';
        image.style.top = '50%';
        image.style.left = '50%';
        image.style.maxWidth = '900px';
        image.style.height = 'auto';
        image.style.transform = 'translate(-50%, -50%)'
        var w = window.open();
        (w as Window).location.href = url;
        setTimeout(() => {
            (w as Window).document.write(image.outerHTML);
            (w as Window).document.body.style.backgroundColor = 'black';
            (w as Window).stop();
        }, 3000);
    }
}

export module HelperService {
    export enum HealthcareTab {
        ClientInfo = 1,
        FileCabinet,
    }
    export enum HealthcareLessonGraphTab {
        Intervention = 1,
        Maintenance = 2
    }
}
