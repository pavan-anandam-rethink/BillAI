import { Component, EventEmitter, Output, ViewEncapsulation } from "@angular/core";
import { Observable, Subject, of } from "rxjs";
import { AttachmentService, IUploadResult } from "@core/services/billing/attachment.service";
import { takeUntil } from "rxjs/operators";
import { UploadFile } from "@app/billing/payment-posting/payment-posting-view/attachments/add-attachment-dialog/add-attachment-dialog.component";
import { EncounterAttachmentService } from "@core/services/billing/encounter-attachment.service";
import { AccountMemberService } from "@core/services/account/account-member.service";

@Component({
    selector: 'eob-upload',
    templateUrl: './eob-upload.component.html',
    styleUrls: ['./eob-upload.component.css'],
    encapsulation: ViewEncapsulation.None
})
export class EobUploadComponent {
    @Output() fileSelected: EventEmitter<{ fileName: string, file: File } | null> = new EventEmitter<{ fileName: string, file: File } | null>();

    private unsubscribeAll$ = new Subject<void>();

    files: UploadFile[] = [];
    pendingFile: File | null = null;
    isDraggedOver: boolean = false;
    unsupportedFileMessage: string = '';
    isUploading: boolean = false;

    // EOB files are typically PDFs or images
    allowedTypes = ["application/pdf", "image/jpeg", "image/png", "image/bmp", "image/gif"];

    constructor(
        private attachmentService: AttachmentService,
        private encounterAttachmentService: EncounterAttachmentService,
        private accountService: AccountMemberService
    ) {}

    public remove(upload: any, uid: string) {
        upload.removeFilesByUid(uid);
    }

    hasFile(): boolean {
        return this.files.length > 0;
    }

    uploadFiles(files: any[]) {
        this.unsupportedFileMessage = '';
        for (const file of files) {
            if (this.allowedTypes.indexOf(file.type) === -1) {
                this.unsupportedFileMessage = 'Supported file types are .pdf, .jpeg, .png, .bmp, .gif';
                continue;
            }

            // Store file locally without uploading
            this.pendingFile = file;
            this.files.push(file);
            file.isSuccessful = true; // Mark as ready (not uploaded yet)
            this.fileSelected.emit({ fileName: file.name, file: file });
        }

        this.onFileDragLeave();
    }

    // Called by parent component after payment record is created
    uploadFile(paymentPostingId: number): Observable<{ fileName: string, fileId: number } | null> {
        if (!this.pendingFile) {
            return of(null);
        }

        const file = this.pendingFile;
        
        // Build payload matching the existing API format
        let fileToUpload = {
            FileName: file.name,
            FileMimeType: file.type,
            ClaimId: 0,
            paymentId: paymentPostingId,
            Data: '',
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        };

        this.isUploading = true;

        return new Observable(observer => {
            let reader = new FileReader();
            let path = '/PaymentAttachment/UploadFile';

            reader.onload = () => {
                const dataUrl = reader.result.toString();
                var resultArr = dataUrl.split(',');
                fileToUpload.Data = resultArr[resultArr.length - 1];
                
                this.encounterAttachmentService.uploadFiles(fileToUpload, path)
                    .pipe(takeUntil(this.unsubscribeAll$))
                    .subscribe((x: IUploadResult) => {
                        switch (x.status) {
                            case "progress":
                                const uploadFile = this.files.find(f => f.name === file.name);
                                if (uploadFile) {
                                    uploadFile.progress = x.progress;
                                }
                                break;
                            case "done":
                                const doneFile = this.files.find(f => f.name === file.name);
                                if (doneFile) {
                                    doneFile.id = parseInt(x.result);
                                    doneFile.isSuccessful = true;
                                }
                                this.isUploading = false;
                                this.pendingFile = null;
                                observer.next({ fileName: file.name, fileId: parseInt(x.result) });
                                observer.complete();
                                break;
                            default:
                                break;
                        }
                    }, error => {
                        const errorFile = this.files.find(f => f.name === file.name);
                        if (errorFile) {
                            errorFile.isSuccessful = false;
                        }
                        this.isUploading = false;
                        observer.error(error);
                    });
            };
            reader.onerror = (error) => {
                this.isUploading = false;
                observer.error(error);
            };
            reader.readAsDataURL(file);
        });
    }

    hasPendingFile(): boolean {
        return this.pendingFile !== null;
    }

    onFileDropped(files: any) {
        if (this.hasFile())
            return;

        this.uploadFiles(files);
    }

    fileBrowseHandler(event: any) {
        if (this.hasFile())
            return;

        const files = event.target.files;

        this.uploadFiles(files);
    }

    onFileDragOver() {
        this.isDraggedOver = true;
    }

    onFileDragLeave() {
        this.isDraggedOver = false;
    }

    deleteAttachment(file: UploadFile) {
        // If file was already uploaded to server, delete from server
        if (file.id != undefined) {
            this.attachmentService.deleteEraUpload(file.id)
                .pipe(takeUntil(this.unsubscribeAll$))
                .subscribe(x => {
                    this.files.remove(file);
                    this.pendingFile = null;
                    this.fileSelected.emit(null);
                });
        } else {
            // File not yet uploaded, just remove locally
            this.files.remove(file);
            this.pendingFile = null;
            this.fileSelected.emit(null);
        }
    }

    deleteAttachments() {
        let fileToDelete = this.files.first((x: UploadFile) => x.isSuccessful);

        if (fileToDelete != undefined) {
            this.deleteAttachment(fileToDelete);
        }
    }

    removeAttachment(file: UploadFile) {
        this.files.remove(file);
        this.pendingFile = null;
        this.fileSelected.emit(null);
    }

    clearFiles() {
        this.files = [];
        this.pendingFile = null;
    }

    getSize(bytes: number) {
        if (bytes < 1000)
            return `${bytes.toFixed(2)} B`;

        bytes = bytes / 1024;

        if (bytes < 1000)
            return `${bytes.toFixed(2)} KB`;

        bytes = bytes / 1024;

        return `${bytes.toFixed(2)} MB`;
    }

    ngOnDestroy() {
        this.unsubscribeAll$.next(void 0);
        this.unsubscribeAll$.complete();
    }
}
