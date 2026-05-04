export class ClaimAttachment {
    public id: number;
    public fileName: string;
    public fileSize: number | null | undefined;
    public fileMimeType: string;
    public notes: string;
    public encounterAttachmentTypeId: number;
    public encounterAttachmentTypeName: string;
    public dateCreated: Date;
    public claimId: number;
    public filePath: string;
    public fileLink: string;

    constructor() {
        this.id = 0;
        this.fileName = "";
        this.fileSize = 0;
        this.fileMimeType = "";
        this.notes = "";
        this.encounterAttachmentTypeId = 0;
        this.encounterAttachmentTypeName = "";
        this.dateCreated = new Date();
        this.claimId = 0;
        this.filePath = "";
        this.fileLink = "";
    }
}