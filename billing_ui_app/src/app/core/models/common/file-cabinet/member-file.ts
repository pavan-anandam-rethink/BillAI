import { FileTag } from "../file-tag/file-tag";

export interface MemberFile {
    companyFile: boolean;
    createdBy: string;
    dateUploaded: Date;
    fileId: number;
    fileMimeType: string;
    fileName: string;
    filePath: string;
    fileType: string;
    folderId: number;
    isParent: boolean;
    originalFileName: string;
    folderName: string;
    fileTags: FileTag[];
    effectiveDate?: Date;
    expirationDate?: Date;
}

export interface FilesInfo {
    files: MemberFile[];
    folderInfo: any;
}