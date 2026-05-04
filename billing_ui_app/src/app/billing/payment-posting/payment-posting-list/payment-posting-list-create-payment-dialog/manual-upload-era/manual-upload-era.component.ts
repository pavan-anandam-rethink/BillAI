import {Component, EventEmitter, Output, ViewEncapsulation} from "@angular/core";
import {Subject} from "rxjs";
import {AttachmentService, IUploadResult} from "@core/services/billing/attachment.service";
import {takeUntil} from "rxjs/operators";
import { UploadFile } from "@app/billing/payment-posting/payment-posting-view/attachments/add-attachment-dialog/add-attachment-dialog.component";
import { FileToUpload } from "@core/services/billing/encounter-attachment.service";

import {EncounterAttachmentService} from "@core/services/billing/encounter-attachment.service";
import { AccountMemberService } from "@core/services/account/account-member.service";

@Component({
    selector: 'manual-upload-era',
    templateUrl: './manual-upload-era.html',
    styleUrls: ['./manual-upload-era.css'],
    encapsulation: ViewEncapsulation.None
})

export class ManualUploadEraComponent {
    @Output() fileIdChanged: EventEmitter<{fileName: string, fileId: number}> = new EventEmitter<{fileName: string, fileId: number}>();

    private unsubscribeAll$ = new Subject<void>();

    files: UploadFile[] = [];
    isDraggedOver: boolean = false;
    unsupportedFileMessage: string='';

    allowedTypes = ["text/plain"];

    constructor(private attachmentService: AttachmentService, private encounterAttachmentService: EncounterAttachmentService, private accountService: AccountMemberService) {
    }

    public remove(upload: any, uid: string) {
        upload.removeFilesByUid(uid);
    }

    uploadFiles(files: any[]) {
        this.unsupportedFileMessage=''; 
        for (const file of files) {
            if (this.allowedTypes.indexOf(file.type) === -1) {
                this.unsupportedFileMessage='Supported file types are .txt,.pdf,.jpeg,.png,.bmp,.gif,.doc/.docx,.xls/.xlsx,.ppt/.pptx';                
                continue;
            }           

            this.files.push(file);
            this.uploadFile(file);
        }


        this.onFileDragLeave();
    }

    uploadFile(file: UploadFile) {
        let fileToUpload = new FileToUpload();
        fileToUpload.AccountInfoId = this.accountService.memberDetails.accountInfoId;
        fileToUpload.MemberId =  this.accountService.memberDetails.memberId;

        file.progress = 0;

        fileToUpload.FileName = file.name;
        fileToUpload.FileMimeType = file.type;

	    let reader = new FileReader();
        let path = '/PaymentPosting/UploadFile';

        reader.onload = () => {

            fileToUpload.Data = reader.result.toString();
                var resultArr = fileToUpload.Data.split(',');
                fileToUpload.Data = resultArr[resultArr.length-1];
                this.encounterAttachmentService.uploadFiles(fileToUpload, path)
                .pipe(takeUntil(this.unsubscribeAll$))
                .subscribe((x: IUploadResult) => {
                    switch (x.status) {
                        case "progress":
                            file.progress = x.progress;
                            break;
                        case "done":
                            file.id = parseInt(x.result);
                            file.isSuccessful = true;
                            this.fileIdChanged.emit({fileName: file.name, fileId: file.id});
                            break;
                        default:
                            break;
                    }
                }, error => {
                    file.isSuccessful = false;
                });
        }
        reader.readAsDataURL(file);
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
        if (file.id != undefined) {
            this.attachmentService.deleteEraUpload(file.id)
                .pipe(takeUntil(this.unsubscribeAll$))
                .subscribe(x => {
                    this.files.remove(file);
                    if(file.isSuccessful){
                        this.fileIdChanged.emit();
                    }
                });
        }
    }
    
    deleteAttachments() {
        let fileToDelete = this.files.first((x: UploadFile) => x.isSuccessful);
        
        if(fileToDelete != undefined) {
            this.deleteAttachment(fileToDelete);
        }
    }

    removeAttachment(file: UploadFile) {
        this.files.remove(file);
    }

    getSize(bytes: number) {
        if (bytes < 1000)
            return `${bytes.toFixed(2)} B`

        bytes = bytes / 1024;

        if (bytes < 1000)
            return `${bytes.toFixed(2)} KB`

        bytes = bytes / 1024;

        return `${bytes.toFixed(2)} MB`

    }

    hasFile() {
        return this.files.any((x: UploadFile) => x.isSuccessful);
    }

    ngOnDestroy(): void {
        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }
}