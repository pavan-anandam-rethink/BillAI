import {Component, EventEmitter, HostListener, Input, OnDestroy, Output, ViewChild, ViewEncapsulation} from "@angular/core";
import {AttachmentService, IUploadResult} from "@core/services/billing/attachment.service";
import {Subject} from "rxjs";
import {takeUntil} from "rxjs/operators";
import {EncounterAttachmentService, FileToUpload} from "@core/services/billing/encounter-attachment.service";

import { IdWithUserInfo } from "@core/models/billing/get-claim-by-identifier";
import { NotificationService } from "@progress/kendo-angular-notification";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";

export class UploadFile extends File {
    id: number;
    progress: number;
    isLoading: boolean;
    isSuccessful: boolean;
}


@Component({
    selector: 'add-attachment-dialog',
    templateUrl: './add-attachment-dialog.html',
    styleUrls: ['./add-attachment-dialog.css'],
    encapsulation: ViewEncapsulation.None
})

export class AddAttachmentDialogComponent implements OnDestroy {
    @ViewChild('fileDropRef') fileInput: any;
    @Input() paymentId: number;
    @Input() existingFiles:string[]; 
    @Output() closeDialogEmitter = new EventEmitter();
    @Output() fileIdChanged: EventEmitter<{fileName: string, fileId: number}> = new EventEmitter<{fileName: string, fileId: number}>();

    private unsubscribeAll$ = new Subject<void>();
    
    files: UploadFile[] = [];
    isDraggedOver: boolean = false;
    unsupportedFileMessage: string='';
    fileLength:number =0;

    allowedTypes = ["application/msword",
        "application/vnd.ms-excel",
        "application/vnd.ms-powerpoint",
        "text/plain",
        "application/pdf",
        "image/bmp",
        "image/gif",
        "image/jpeg",
        "image/png",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"];

    constructor(private attachmentService: AttachmentService,private notificationService: NotificationHandlerService, private encounterAttachmentService: EncounterAttachmentService
        , private accountService: AccountMemberService) {
    }

    onCloseDialog(): void {
        this.closeDialogEmitter.emit();
    }

    public remove(upload: any, uid: string) {
        upload.removeFilesByUid(uid);
    }

    uploadFiles(files: any[]) {
        this.unsupportedFileMessage=''; 
        let alreadyAttached=0;
        for (const file of files) {
            if(this.allowedTypes.indexOf(file.type) === -1){
                this.unsupportedFileMessage='Supported file types are .txt,.pdf,.jpeg,.png,.bmp,.gif,.doc/.docx,.xls/.xlsx,.ppt/.pptx';                
                continue;
            }
            
            if (!(this.files.some(element => element.name == file.name) || this.existingFiles.some(element => element == file.name))) {
                this.fileLength += 1;
                this.files.push(file);
            } 
            else{ alreadyAttached +=1}
        }
        
        if (this.fileLength > 0) {
            this.files.forEach(file => {
                this.uploadFile(file);
            })
        }
        if(alreadyAttached>0) {
            this.notificationService.showNotificationWarning(alreadyAttached +" Attachment(s) already uploaded.");
        }
        if(this.fileLength)
        {
            this.notificationService.showNotificationSuccess(this.fileLength + " Attachement(s) added successfully.");
        }
        this.fileLength = 0;
        alreadyAttached = 0;
        this.onFileDragLeave();
    }

    async uploadFile(file: UploadFile){
        this.fileInput.nativeElement.value = '';
        let fileToUpload = new FileToUpload();
        fileToUpload.AccountInfoId = this.accountService.memberDetails.accountInfoId;
        fileToUpload.MemberId =  this.accountService.memberDetails.memberId;

        file.isLoading = true;
        file.progress = 0;
        fileToUpload.FileName = file.name;
        fileToUpload.FileMimeType = file.type;
        fileToUpload.paymentId = this.paymentId;

        let reader = new FileReader();
        let path = '/PaymentAttachment/UploadFile';

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
                        file.isLoading = false;
                        this.fileIdChanged.emit({fileName: file.name, fileId: file.id});
                        break;
                    default:
                        break;
                        
                }
                
            }, error => {
                file.isLoading = false;
                file.isSuccessful = false;
            });
        }
        reader.readAsDataURL(file);
    }

    onFileDropped(files: any) {
        this.uploadFiles(files);
    }

    fileBrowseHandler(event: any) {
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
        const deleteAttachmentModel: IdWithUserInfo = {
            Id: file.id,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        };
        if (file.id != undefined) {
            this.attachmentService.deleteUpload(deleteAttachmentModel)
                .pipe(takeUntil(this.unsubscribeAll$))
                .subscribe(x => {
                    this.files.remove(file);
                    this.notificationService.showNotificationSuccess("Attachment(s) removed successfully.")
                });
        }
    }
    
    removeAttachment(file: UploadFile){
        this.files.remove(file);
        
    }

    cancel() {
        this.onCloseDialog();
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

    ngOnDestroy(): void {
        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }
} 