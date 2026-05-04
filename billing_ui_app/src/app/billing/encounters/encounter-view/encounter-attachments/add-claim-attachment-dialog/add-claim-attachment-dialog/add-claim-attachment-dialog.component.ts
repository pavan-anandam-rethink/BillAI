import {Component, EventEmitter,Input, OnDestroy, Output, ViewChild} from "@angular/core";
import {Subject} from "rxjs";
import {takeUntil} from "rxjs/operators";
import {EncounterAttachmentService} from "@core/services/billing";
import { FileToUpload, IUploadResult } from "@core/services/billing/encounter-attachment.service";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";
import { IdWithUserInfo } from "@core/models/billing/get-claim-by-identifier";



export class UploadFile extends File {
    id: number;
    progress: number;
    isLoading: boolean;
    isDeleting: boolean;
    isSuccessful: boolean;
}

@Component({
    selector: 'add-claim-attachment-dialog',
    templateUrl: './add-claim-attachment-dialog.component.html',
    styleUrls: ['./add-claim-attachment-dialog.component.css']
})

export class AddClaimAttachmentDialogComponent implements OnDestroy {
    @ViewChild('fileDropRef') fileInput: any;
    @Input() claimId: number;
    @Input() existingFiles:string[]; 
    @Output() closeDialogEmitter = new EventEmitter();

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

    constructor(private encounterAttachmentService: EncounterAttachmentService,
        private notificationService: NotificationHandlerService, 
        private accountService: AccountMemberService) {
        
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
            if (this.allowedTypes.indexOf(file.type) === -1) {
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
        alreadyAttached =0;
        this.onFileDragLeave();
    }

    async uploadFile(file: UploadFile) {
        this.fileInput.nativeElement.value = '';
        let fileToUpload = new FileToUpload();
        fileToUpload.AccountInfoId = this.accountService.memberDetails.accountInfoId;
        fileToUpload.MemberId =  this.accountService.memberDetails.memberId;

        file.isLoading = true;
        file.progress = 0;

        fileToUpload.FileName = file.name;
        fileToUpload.FileMimeType = file.type;
        fileToUpload.ClaimId = this.claimId;

        let reader = new FileReader();

        let path = '/ClaimAttachment/UploadFile';

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
        if(file.isDeleting)
            return;
        
        file.isDeleting = true;
        if (file.id != undefined) {
            var req: IdWithUserInfo = { Id: file.id, AccountInfoId: this.accountService.memberDetails.accountInfoId, MemberId: this.accountService.memberDetails.memberId };
            this.encounterAttachmentService.deleteUpload(req)
                .pipe(takeUntil(this.unsubscribeAll$))
                .subscribe(x => {
                        this.files.remove(file);
                    },
                    error => {
                    },
                    () => {
                        file.isDeleting = false;
                    });
        }
        this.notificationService.showNotificationSuccess("Attachment(s) removed successfully.");
    }

    removeAttachment(file: UploadFile) {
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
