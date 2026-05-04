export class EncounterAttachmentFile {
    public uploadedFilePath: string;
    public attachmentFileName: string;
    public attachmentFileSize: number | null | undefined = 0;
    public attachmentFileMimeType: string;
    public attachmentFileLink: string;

    constructor() {
        this.uploadedFilePath = '';
        this.attachmentFileName = '';
        this.attachmentFileSize = 0;
        this.attachmentFileMimeType = '';
        this.attachmentFileLink = '';
    }
}