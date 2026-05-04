export class PrintInvoiceShared {
    public processPDF(response: any) {
        const blob = this.base64ToBlob(response.pdfBase64, 'application/pdf');
        const blobUrl = URL.createObjectURL(blob);
        const iframe = document.createElement('iframe');
        document.body.appendChild(iframe);

        iframe.style.display = 'none';
        iframe.src = blobUrl;
        iframe.onload = function () {
            window.setTimeout(function () {
                iframe.focus();
                iframe.contentWindow && iframe.contentWindow.print();
            }, 100);
        }
    }

    private base64ToBlob(base64: string, contentType: string): Blob {
        const byteCharacters = atob(base64); 
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
          byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        return new Blob([byteArray], { type: contentType });
      }
}